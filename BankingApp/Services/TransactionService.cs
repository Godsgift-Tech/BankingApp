using BankingApp.Application.DTO.Common;
using BankingApp.Application.DTO.Transactions;
using BankingApp.Application.Interfaces.Repository;
using BankingApp.Application.Interfaces.Services;
using BankingApp.Core.Entities;
using BankingApp.Core.Enums;
using Microsoft.Extensions.Caching.Distributed;
using Serilog;
using StackExchange.Redis;
using System.Text.Json;

namespace BankingApp.Application.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly ITransactionRepository _transactionRepository;
        private readonly IDistributedCache _cache;
        private readonly IConnectionMultiplexer _redis;

        public TransactionService(
            ITransactionRepository transactionRepository,
            IDistributedCache cache,
            IConnectionMultiplexer redis)
        {
            _transactionRepository = transactionRepository;
            _cache = cache;
            _redis = redis;
        }

        public async Task DepositAsync(DepositDto dto)
        {
            Log.Information("Initiating deposit: AccountId={AccountId}, Amount={Amount}", dto.AccountId, dto.Amount);

            var account = await _transactionRepository.GetAccountByIdAsync(dto.AccountId);
            if (account == null)
            {
                Log.Warning("Deposit failed: Account not found. AccountId={AccountId}", dto.AccountId);
                throw new Exception("Account not found");
            }

            account.Balance += dto.Amount;

            var transaction = new Transaction
            {
                AccountId = dto.AccountId,
                Amount = dto.Amount,
                Description = dto.Description,
                Type = TransactionType.Deposit,
                Status = TransactionStatus.Success
            };

            await _transactionRepository.UpdateAccountAsync(account);
            await _transactionRepository.AddTransactionAsync(transaction);
            await _transactionRepository.SaveChangesAsync();

            Log.Information("Deposit successful: AccountId={AccountId}, NewBalance={Balance}", dto.AccountId, account.Balance);

            await InvalidateTransactionHistoryCache(dto.AccountId);
        }

        public async Task WithdrawAsync(WithdrawDto dto)
        {
            Log.Information("Initiating withdrawal: AccountId={AccountId}, Amount={Amount}", dto.AccountId, dto.Amount);

            var account = await _transactionRepository.GetAccountByIdAsync(dto.AccountId);
            if (account == null)
            {
                Log.Warning("Withdrawal failed: Account not found. AccountId={AccountId}", dto.AccountId);
                throw new Exception("Account not found");
            }

            if (account.Balance < dto.Amount)
            {
                Log.Warning("Withdrawal failed: Insufficient funds. AccountId={AccountId}, Balance={Balance}, AttemptedAmount={Amount}",
                    dto.AccountId, account.Balance, dto.Amount);
                throw new Exception("Insufficient balance");
            }

            account.Balance -= dto.Amount;

            var transaction = new Transaction
            {
                AccountId = dto.AccountId,
                Amount = dto.Amount,
                Description = dto.Description,
                Type = TransactionType.Withdrawal,
                Status = TransactionStatus.Success
            };

            await _transactionRepository.UpdateAccountAsync(account);
            await _transactionRepository.AddTransactionAsync(transaction);
            await _transactionRepository.SaveChangesAsync();

            Log.Information("Withdrawal successful: AccountId={AccountId}, NewBalance={Balance}", dto.AccountId, account.Balance);

            await InvalidateTransactionHistoryCache(dto.AccountId);
        }

        public async Task TransferAsync(TransferDto dto)
        {
            Log.Information("Initiating transfer: FromAccountId={FromAccountId}, ToAccountNumber={ToAccountNumber}, Amount={Amount}",
                dto.FromAccountId, dto.ToAccountNumber, dto.Amount);

            var fromAccount = await _transactionRepository.GetAccountByIdAsync(dto.FromAccountId);
            var toAccount = await _transactionRepository.GetAccountByNumberAsync(dto.ToAccountNumber);

            if (fromAccount == null || toAccount == null)
            {
                Log.Warning("Transfer failed: One or both accounts not found. From={FromAccountId}, To={ToAccountNumber}",
                    dto.FromAccountId, dto.ToAccountNumber);
                throw new Exception("One or both accounts not found");
            }

            if (fromAccount.Balance < dto.Amount)
            {
                Log.Warning("Transfer failed: Insufficient funds. FromAccountId={FromAccountId}, Balance={Balance}, Amount={Amount}",
                    dto.FromAccountId, fromAccount.Balance, dto.Amount);
                throw new Exception("Insufficient balance");
            }

            fromAccount.Balance -= dto.Amount;
            toAccount.Balance += dto.Amount;

            var debit = new Transaction
            {
                AccountId = fromAccount.Id,
                Amount = dto.Amount,
                Description = dto.Description,
                Type = TransactionType.Transfer,
                TargetAccountNumber = toAccount.AccountNumber,
                Status = TransactionStatus.Success
            };

            var credit = new Transaction
            {
                AccountId = toAccount.Id,
                Amount = dto.Amount,
                Description = "Received Transfer",
                Type = TransactionType.Deposit,
                Status = TransactionStatus.Success
            };

            await _transactionRepository.UpdateAccountAsync(fromAccount);
            await _transactionRepository.UpdateAccountAsync(toAccount);
            await _transactionRepository.AddTransactionAsync(debit);
            await _transactionRepository.AddTransactionAsync(credit);
            await _transactionRepository.SaveChangesAsync();

            Log.Information("Transfer successful: From={FromAccountId}, To={ToAccountId}, Amount={Amount}", fromAccount.Id, toAccount.Id, dto.Amount);

            await InvalidateTransactionHistoryCache(fromAccount.Id);
            await InvalidateTransactionHistoryCache(toAccount.Id);
        }

        public async Task<PagedResult<TransactionHistoryDto>> GetTransactionHistoryAsync(
            Guid accountId, int page, int pageSize, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken)
        {
            string cacheKey = $"transactions:{accountId}:{page}:{pageSize}:{fromDate?.ToString("s")}:{toDate?.ToString("s")}";
            Log.Information("Retrieving transaction history for AccountId={AccountId}, Page={Page}", accountId, page);

            var cachedData = await _cache.GetStringAsync(cacheKey, cancellationToken);
            if (!string.IsNullOrEmpty(cachedData))
            {
                Log.Information("Transaction history cache hit for AccountId={AccountId}, Page={Page}", accountId, page);
                return JsonSerializer.Deserialize<PagedResult<TransactionHistoryDto>>(cachedData)!;
            }

            var (transactions, totalCount) = await _transactionRepository.GetPagedTransactionsByAccountIdAsync(
                accountId, page, pageSize, fromDate, toDate, cancellationToken);

            var result = new PagedResult<TransactionHistoryDto>
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = totalCount,
                Items = transactions.Select(t => new TransactionHistoryDto
                {
                    Id = t.Id,
                    Amount = t.Amount,
                    Description = t.Description!,
                    Timestamp = t.Timestamp,
                    Type = t.Type,
                    Status = t.Status,
                    TargetAccountNumber = t.TargetAccountNumber
                }).ToList()
            };

            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            };

            var json = JsonSerializer.Serialize(result);
            await _cache.SetStringAsync(cacheKey, json, options, cancellationToken);

            Log.Information("Transaction history retrieved and cached. AccountId={AccountId}, Page={Page}", accountId, page);

            return result;
        }

        private async Task InvalidateTransactionHistoryCache(Guid accountId)
        {
            try
            {
                var server = _redis.GetServer(_redis.GetEndPoints().First());
                var db = _redis.GetDatabase();
                var pattern = $"transactions:{accountId}*";

                var keys = server.Keys(pattern: pattern).ToArray();

                foreach (var key in keys)
                {
                    await db.KeyDeleteAsync(key);
                    Log.Information("Deleted transaction cache key: {Key}", key);
                }

                if (keys.Length == 0)
                {
                    Log.Information("No transaction cache keys found to delete for AccountId={AccountId}", accountId);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to invalidate transaction cache for AccountId={AccountId}", accountId);
            }
        }
    }
}
