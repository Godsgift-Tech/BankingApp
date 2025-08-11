using BankingApp.Core.Entities;
using BankingAPP.Applications.Features.Common.Interfaces;
using BankingAPP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace BankingAPP.Infrastructure.Repositories
{
    public class AccountRepository : IAccountRepository
    {
        private readonly BankingDbContext _context;
        private readonly IDistributedCache _cache;

        public AccountRepository(BankingDbContext context, IDistributedCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public async Task<Account?> GetAccountByIdWithTransactionsAsync(Guid accountId, CancellationToken cancellationToken)
        {
            var cacheKey = $"account:{accountId}:with-transactions";
            var cachedData = await _cache.GetStringAsync(cacheKey, cancellationToken);

            if (!string.IsNullOrEmpty(cachedData))
                return JsonSerializer.Deserialize<Account>(cachedData);

            var account = await _context.Accounts
                .Include(a => a.Transactions)
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == accountId, cancellationToken);

            if (account != null)
            {
                await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(account),
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                    },
                    cancellationToken);
            }

            return account;
        }

        public async Task<Account?> GetByIdAsync(Guid accountId, CancellationToken cancellationToken)
        {
            var cacheKey = $"account:{accountId}";
            var cachedData = await _cache.GetStringAsync(cacheKey, cancellationToken);

            if (!string.IsNullOrEmpty(cachedData))
                return JsonSerializer.Deserialize<Account>(cachedData);

            var account = await _context.Accounts
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == accountId, cancellationToken);

            if (account != null)
            {
                await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(account),
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                    },
                    cancellationToken);
            }

            return account;
        }

        public async Task<IEnumerable<Account>> GetAllAsync(CancellationToken cancellationToken)
        {
            var cacheKey = "accounts:all";
            var cachedData = await _cache.GetStringAsync(cacheKey, cancellationToken);

            if (!string.IsNullOrEmpty(cachedData))
                return JsonSerializer.Deserialize<IEnumerable<Account>>(cachedData) ?? Enumerable.Empty<Account>();

            var accounts = await _context.Accounts
                .Include(a => a.User)
                .ToListAsync(cancellationToken);

            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(accounts),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                },
                cancellationToken);

            return accounts;
        }

        public async Task AddAsync(Account account, CancellationToken cancellationToken)
        {
            await _context.Accounts.AddAsync(account, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            await _cache.RemoveAsync("accounts:all", cancellationToken);
        }

        public async Task UpdateAsync(Account account, CancellationToken cancellationToken)
        {
            _context.Accounts.Update(account);
            await _context.SaveChangesAsync(cancellationToken);

            await _cache.RemoveAsync($"account:{account.Id}", cancellationToken);
            await _cache.RemoveAsync($"account:{account.Id}:with-transactions", cancellationToken);
            await _cache.RemoveAsync("accounts:all", cancellationToken);
        }

        public async Task DeleteAsync(Account account, CancellationToken cancellationToken)
        {
            _context.Accounts.Remove(account);
            await _context.SaveChangesAsync(cancellationToken);

            await _cache.RemoveAsync($"account:{account.Id}", cancellationToken);
            await _cache.RemoveAsync($"account:{account.Id}:with-transactions", cancellationToken);
            await _cache.RemoveAsync("accounts:all", cancellationToken);
        }
    }
}
