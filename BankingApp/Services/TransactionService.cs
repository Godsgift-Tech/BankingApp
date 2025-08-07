using BankingApp.Application.DTO.Common;
using BankingApp.Application.DTO.Transactions;
using BankingApp.Application.Interfaces.Repository;
using BankingApp.Application.Interfaces.Services;
using BankingApp.Core.Entities;
using BankingApp.Core.Enums;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;
using System.Text.Json;

namespace BankingApp.Application.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly ITransactionRepository _transactionRepository;
        private readonly IDistributedCache _cache;
        private readonly IDatabase _redisDb;
        private readonly IServer _redisServer;

        public TransactionService(
            ITransactionRepository transactionRepository,
            IDistributedCache cache,
            IConnectionMultiplexer redis)
        {
            _transactionRepository = transactionRepository;
            _cache = cache;
            _redisDb = redis.GetDatabase();
            _redisServer = redis.GetServer(redis.GetEndPoints().First());
        }

        public async Task DepositAsync(DepositDto dto)
        {
            var account = await _transactionRepository.GetAccountByIdAsync(dto.AccountId)
                          ?? throw new Exception("Account not found");

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

            await InvalidateTransactionHistoryCache(dto.AccountId);
        }

        public async Task WithdrawAsync(WithdrawDto dto)
        {
            var account = await _transactionRepository.GetAccountByIdAsync(dto.AccountId)
                          ?? throw new Exception("Account not found");

            if (account.Balance < dto.Amount)
                throw new Exception("Insufficient balance");

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

            await InvalidateTransactionHistoryCache(dto.AccountId);
        }

        public async Task TransferAsync(TransferDto dto)
        {
            var fromAccount = await _transactionRepository.GetAccountByIdAsync(dto.FromAccountId);
            var toAccount = await _transactionRepository.GetAccountByNumberAsync(dto.ToAccountNumber);

            if (fromAccount == null || toAccount == null)
                throw new Exception("One or both accounts not found");

            if (fromAccount.Balance < dto.Amount)
                throw new Exception("Insufficient balance");

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

            await InvalidateTransactionHistoryCache(fromAccount.Id);
            await InvalidateTransactionHistoryCache(toAccount.Id);
        }

        public async Task<PagedResult<TransactionHistoryDto>> GetTransactionHistoryAsync(
            Guid accountId, int page, int pageSize, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken)
        {
            string cacheKey = $"transactions:{accountId}:{page}:{pageSize}:{fromDate?.ToString("s")}:{toDate?.ToString("s")}";
            var cachedData = await _cache.GetStringAsync(cacheKey, cancellationToken);

            if (!string.IsNullOrEmpty(cachedData))
            {
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

            return result;
        }

        private async Task InvalidateTransactionHistoryCache(Guid accountId)
        {
            string pattern = $"transactions:{accountId}*";

            var keys = _redisServer.Keys(pattern: pattern).ToArray();

            foreach (var key in keys)
            {
                await _redisDb.KeyDeleteAsync(key);
            }
        }
    }
}
