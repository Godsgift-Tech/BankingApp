using BankingApp.Application.DTO.Common;
using BankingApp.Application.DTO.Transactions;
using BankingApp.Application.Interfaces.Repository;
using BankingApp.Application.Interfaces.Services;
using BankingApp.Core.Entities;
using BankingApp.Core.Enums;
using Microsoft.Extensions.Caching.Distributed;
using Serilog;
using System.Text.Json;

namespace BankingApp.Application.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly ITransactionRepository _transactionRepository;
        private readonly IDistributedCache _cache;

        public TransactionService(ITransactionRepository transactionRepository, IDistributedCache cache)
        {
            _transactionRepository = transactionRepository;
            _cache = cache;
        }

        public async Task<Transaction> DepositAsync(DepositDto dto)
        {
            Log.Information("Initiating deposit: AccountId={AccountId}, Amount={Amount}", dto.AccountId, dto.Amount);

            var account = await _transactionRepository.GetAccountByIdAsync(dto.AccountId);
            if (account == null)
                throw new NotFoundException("Account not found");

            account.Balance += dto.Amount;

            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                AccountId = dto.AccountId,
                Amount = dto.Amount,
                Description = dto.Description,
                Timestamp = DateTime.UtcNow,
                Type = TransactionType.Deposit,
                Status = TransactionStatus.Success,
                BalanceAfterTransaction = Math.Round(account.Balance, 2)
            };

            await _transactionRepository.UpdateAccountAsync(account);
            await _transactionRepository.AddTransactionAsync(transaction);
            await _transactionRepository.SaveChangesAsync();

            // Invalidate cache for this account
            await InvalidateTransactionCacheAsync(dto.AccountId, account.AccountNumber);

            return transaction;
        }

        public async Task<Transaction> WithdrawAsync(WithdrawDto dto)
        {
            Log.Information("Initiating withdrawal: AccountId={AccountId}, Amount={Amount}", dto.AccountId, dto.Amount);

            var account = await _transactionRepository.GetAccountByIdAsync(dto.AccountId);
            if (account == null)
                throw new NotFoundException("Account not found");

            if (account.Balance < dto.Amount)
                throw new InsufficientBalanceException("Insufficient balance");

            account.Balance -= dto.Amount;

            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                AccountId = dto.AccountId,
                Amount = dto.Amount,
                Description = dto.Description,
                Timestamp = DateTime.UtcNow,
                Type = TransactionType.Withdrawal,
                Status = TransactionStatus.Success,
                BalanceAfterTransaction = Math.Round(account.Balance, 2)
            };

            await _transactionRepository.UpdateAccountAsync(account);
            await _transactionRepository.AddTransactionAsync(transaction);
            await _transactionRepository.SaveChangesAsync();

            // Invalidate cache
            await InvalidateTransactionCacheAsync(dto.AccountId, account.AccountNumber);

            return transaction;
        }

        public async Task<Transaction> TransferAsync(TransferDto dto)
        {
            Log.Information("Initiating transfer: FromAccountId={FromAccountId}, ToAccountNumber={ToAccountNumber}, Amount={Amount}",
                dto.FromAccountId, dto.ToAccountNumber, dto.Amount);

            var fromAccount = await _transactionRepository.GetAccountByIdAsync(dto.FromAccountId);
            var toAccount = await _transactionRepository.GetAccountByNumberAsync(dto.ToAccountNumber);

            if (fromAccount == null || toAccount == null)
                throw new NotFoundException("One or both accounts not found");

            // Prevent you transfering funds to same account. 
            if (fromAccount.Id == toAccount.Id || fromAccount.AccountNumber == toAccount.AccountNumber)
                throw new InvalidOperationException("Transfers to the same account are not allowed.");

            if (fromAccount.Balance < dto.Amount)
                throw new InsufficientBalanceException("Insufficient balance");

            fromAccount.Balance -= dto.Amount;
            toAccount.Balance += dto.Amount;

            var debit = new Transaction
            {
                Id = Guid.NewGuid(),
                AccountId = fromAccount.Id,
                Amount = dto.Amount,
                Description = dto.Description,
                Timestamp = DateTime.UtcNow,
                Type = TransactionType.Transfer,
                TargetAccountNumber = toAccount.AccountNumber,
                Status = TransactionStatus.Success,
                BalanceAfterTransaction = Math.Round(fromAccount.Balance, 2)
            };

            var credit = new Transaction
            {
                Id = Guid.NewGuid(),
                AccountId = toAccount.Id,
                Amount = dto.Amount,
                Description = "Received Transfer",
                Timestamp = DateTime.UtcNow,
                Type = TransactionType.Deposit,
                Status = TransactionStatus.Success,
                BalanceAfterTransaction = Math.Round(toAccount.Balance, 2)
            };

            await _transactionRepository.UpdateAccountAsync(fromAccount);
            await _transactionRepository.UpdateAccountAsync(toAccount);
            await _transactionRepository.AddTransactionAsync(debit);
            await _transactionRepository.AddTransactionAsync(credit);
            await _transactionRepository.SaveChangesAsync();

            // Invalidate both accounts’ cache
            await InvalidateTransactionCacheAsync(fromAccount.Id, fromAccount.AccountNumber);
            await InvalidateTransactionCacheAsync(toAccount.Id, toAccount.AccountNumber);

            return debit;
        }

        public async Task<PagedResult<TransactionHistoryDto>> GetTransactionHistoryByAccountIdAsync(
            Guid accountId, int page, int pageSize, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken)
        {
            string cacheKey = $"txn_history_id_{accountId}_p{page}_s{pageSize}_{fromDate?.Ticks}_{toDate?.Ticks}";

            var cachedData = await _cache.GetStringAsync(cacheKey, cancellationToken);
            if (!string.IsNullOrEmpty(cachedData))
            {
                Log.Information("Transaction history (AccountId) loaded from cache.");
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
                    Description = t.Description ?? string.Empty,
                    Timestamp = t.Timestamp,
                    Type = t.Type.ToString(),
                    Status = t.Status.ToString(),
                    TargetAccountNumber = t.TargetAccountNumber,
                    BalanceAfterTransaction = Math.Round(t.BalanceAfterTransaction, 2)
                }).ToList()
            };

            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(result),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) }, cancellationToken);

            return result;
        }

        public async Task<PagedResult<TransactionHistoryDto>> GetAccountHistoryByAccountNumberAsync(
            string accountNumber, int pageNumber, int pageSize, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken)
        {
            string cacheKey = $"txn_history_num_{accountNumber}_p{pageNumber}_s{pageSize}_{fromDate?.Ticks}_{toDate?.Ticks}";

            var cachedData = await _cache.GetStringAsync(cacheKey, cancellationToken);
            if (!string.IsNullOrEmpty(cachedData))
            {
                Log.Information("Transaction history (AccountNumber) loaded from cache.");
                return JsonSerializer.Deserialize<PagedResult<TransactionHistoryDto>>(cachedData)!;
            }

            var (transactions, totalCount) = await _transactionRepository.GetPagedTransactionsByAccountNumberAsync(
                accountNumber, pageNumber, pageSize, fromDate, toDate, cancellationToken);

            var result = new PagedResult<TransactionHistoryDto>
            {
                Page = pageNumber,
                PageSize = pageSize,
                TotalItems = totalCount,
                Items = transactions.Select(t => new TransactionHistoryDto
                {
                    Id = t.Id,
                    Amount = t.Amount,
                    Description = t.Description ?? string.Empty,
                    Timestamp = t.Timestamp,
                    Type = t.Type.ToString(),
                    Status = t.Status.ToString(),
                    TargetAccountNumber = t.TargetAccountNumber,
                    BalanceAfterTransaction = Math.Round(t.BalanceAfterTransaction, 2)
                }).ToList()
            };

            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(result),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) }, cancellationToken);

            return result;
        }

        private async Task InvalidateTransactionCacheAsync(Guid accountId, string accountNumber)
        {
            try
            {
                // For simplicity, remove all possible keys for this account
                await _cache.RemoveAsync($"txn_history_id_{accountId}");
                await _cache.RemoveAsync($"txn_history_num_{accountNumber}");
                Log.Information("Cache invalidated for AccountId={AccountId}, AccountNumber={AccountNumber}", accountId, accountNumber);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to invalidate transaction cache.");
            }
        }
    }

    public class InsufficientBalanceException : Exception
    {
        public InsufficientBalanceException(string message) : base(message) { }
    }

    public class NotFoundException : Exception
    {
        public NotFoundException(string message) : base(message) { }
    }
}
