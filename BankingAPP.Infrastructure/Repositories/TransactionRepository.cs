using BankingApp.Core.Entities;
using BankingAPP.Applications.Features.Common.Interfaces;
using BankingAPP.Applications.Features.Transactions.DTO;
using BankingAPP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Serilog;
using System.Text.Json;

namespace BankingAPP.Infrastructure.Repositories
{
    public class TransactionRepository : ITransactionRepository
    {
        private readonly BankingDbContext _context;
        private readonly IDistributedCache _cache;

        public TransactionRepository(BankingDbContext context, IDistributedCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public async Task<Transaction?> GetByIdAsync(Guid transactionId, CancellationToken cancellationToken)
        {
            var cacheKey = $"transaction:{transactionId}";
            var cachedTransaction = await _cache.GetStringAsync(cacheKey, cancellationToken);

            if (!string.IsNullOrEmpty(cachedTransaction))
            {
                return JsonSerializer.Deserialize<Transaction>(cachedTransaction);
            }

            var transaction = await _context.Transactions
                .Include(t => t.Account)
                .FirstOrDefaultAsync(t => t.Id == transactionId, cancellationToken);

            if (transaction != null)
            {
                await _cache.SetStringAsync(
                    cacheKey,
                    JsonSerializer.Serialize(transaction),
                    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) },
                    cancellationToken
                );
            }

            return transaction;
        }

        public async Task<IEnumerable<Transaction>> GetByAccountIdAsync(Guid accountId, CancellationToken cancellationToken)
        {
            var cacheKey = $"transactions:account:{accountId}";
            var cachedTransactions = await _cache.GetStringAsync(cacheKey, cancellationToken);

            if (!string.IsNullOrEmpty(cachedTransactions))
            {
                return JsonSerializer.Deserialize<IEnumerable<Transaction>>(cachedTransactions) ?? Enumerable.Empty<Transaction>();
            }

            var transactions = await _context.Transactions
                .Where(t => t.AccountId == accountId)
                .OrderByDescending(t => t.Timestamp)
                .ToListAsync(cancellationToken);

            await _cache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(transactions),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) },
                cancellationToken
            );

            return transactions;
        }

        public async Task<IEnumerable<Transaction>> GetByAccountIdPagedAsync(Guid accountId, int pageNumber, int pageSize, CancellationToken cancellationToken)
        {
            var cacheKey = $"transactions:account:{accountId}:page:{pageNumber}:size:{pageSize}";
            var cachedTransactions = await _cache.GetStringAsync(cacheKey, cancellationToken);

            if (!string.IsNullOrEmpty(cachedTransactions))
            {
                return JsonSerializer.Deserialize<IEnumerable<Transaction>>(cachedTransactions) ?? Enumerable.Empty<Transaction>();
            }

            var transactions = await _context.Transactions
                .Where(t => t.AccountId == accountId)
                .OrderByDescending(t => t.Timestamp)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            await _cache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(transactions),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) },
                cancellationToken
            );

            return transactions;
        }

        // New method: paged + date range filtering
        public async Task<IEnumerable<Transaction>> GetByAccountIdPagedAsync(
            Guid accountId,
            int pageNumber,
            int pageSize,
            DateTime? fromDate,
            DateTime? toDate,
            CancellationToken cancellationToken)
        {
            var cacheKey = $"transactions:account:{accountId}:page:{pageNumber}:size:{pageSize}:from:{fromDate:yyyyMMdd}:to:{toDate:yyyyMMdd}";
            var cachedTransactions = await _cache.GetStringAsync(cacheKey, cancellationToken);

            if (!string.IsNullOrEmpty(cachedTransactions))
            {
                return JsonSerializer.Deserialize<IEnumerable<Transaction>>(cachedTransactions) ?? Enumerable.Empty<Transaction>();
            }

            var query = _context.Transactions.AsQueryable()
                .Where(t => t.AccountId == accountId);

            if (fromDate.HasValue)
                query = query.Where(t => t.Timestamp >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(t => t.Timestamp <= toDate.Value);

            var transactions = await query
                .OrderByDescending(t => t.Timestamp)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            await _cache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(transactions),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) },
                cancellationToken
            );

            return transactions;
        }

        public async Task AddAsync(Transaction transaction, CancellationToken cancellationToken)
        {
            await _context.Transactions.AddAsync(transaction, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            // Invalidate related account transaction caches
            await _cache.RemoveAsync($"transactions:account:{transaction.AccountId}", cancellationToken);
        }

        public async Task UpdateAsync(Transaction transaction, CancellationToken cancellationToken)
        {
            _context.Transactions.Update(transaction);
            await _context.SaveChangesAsync(cancellationToken);

            await _cache.RemoveAsync($"transaction:{transaction.Id}", cancellationToken);
            await _cache.RemoveAsync($"transactions:account:{transaction.AccountId}", cancellationToken);
        }


        public async Task<List<TransactionHistoryDto>> GetTransactionsAsync(
            Guid accountId,
            DateTime? fromDate,
            DateTime? toDate,
            CancellationToken cancellationToken)
        {
            Log.Information("Fetching transactions for AccountId {AccountId}", accountId);

            var query = _context.Transactions
                .Where(t => t.AccountId == accountId);

            if (fromDate.HasValue)
                query = query.Where(t => t.Timestamp >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(t => t.Timestamp <= toDate.Value);

            return await query
                .OrderByDescending(t => t.Timestamp)
                .Select(t => new TransactionHistoryDto
                {
                    Timestamp = t.Timestamp,
                    Description = t.Description,
                    Amount = t.Amount,
                    BalanceAfterTransaction = t.BalanceAfterTransaction
                })
                .ToListAsync(cancellationToken);
        }

        public async Task DeleteAsync(Transaction transaction, CancellationToken cancellationToken)
        {
            _context.Transactions.Remove(transaction);
            await _context.SaveChangesAsync(cancellationToken);

            await _cache.RemoveAsync($"transaction:{transaction.Id}", cancellationToken);
            await _cache.RemoveAsync($"transactions:account:{transaction.AccountId}", cancellationToken);
        }
    }
}
