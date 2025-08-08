using BankingApp.Application.DTO.Accounts;
using BankingApp.Application.Interfaces.Repository;
using BankingApp.Application.Interfaces.Services;
using BankingApp.Core.Entities;
using Microsoft.Extensions.Caching.Distributed;
using Serilog;
using StackExchange.Redis;
using System.Text.Json;

namespace BankingApp.Application.Services
{
    public class AccountService : IAccountService
    {
        private readonly IAccountRepository _accountRepository;
        private readonly IDistributedCache _cache;
        private readonly IConnectionMultiplexer _redis;

        public AccountService(
            IAccountRepository accountRepository,
            IDistributedCache cache,
            IConnectionMultiplexer redis)
        {
            _accountRepository = accountRepository;
            _cache = cache;
            _redis = redis;
        }

        public async Task<Guid> CreateAccountAsync(CreateAccountDto dto, CancellationToken cancellationToken)
        {
            Log.Information("Attempting to create account for UserId: {UserId}, AccountNumber: {AccountNumber}", dto.UserId, dto.AccountNumber);

            if (await _accountRepository.AccountNumberExistsAsync(dto.AccountNumber, cancellationToken))
            {
                Log.Warning("Account creation failed: Account number {AccountNumber} already exists", dto.AccountNumber);
                throw new Exception("Account number already exists.");
            }

            var account = new Account
            {
                UserId = dto.UserId,
                AccountNumber = dto.AccountNumber,
                CreatedAt = DateTime.UtcNow
            };

            var accountId = await _accountRepository.CreateAccountAsync(account, cancellationToken);

            var accountDto = new AccountDto
            {
                Id = accountId,
                AccountNumber = dto.AccountNumber,
                Balance = 0m,
                CreatedAt = account.CreatedAt
            };

            var cacheKey = $"account:{accountId}";
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
            };

            var serialized = JsonSerializer.Serialize(accountDto);
            await _cache.SetStringAsync(cacheKey, serialized, cacheOptions);

            Log.Information("Account created and cached successfully. AccountId: {AccountId}", accountId);

            await InvalidateAccountCache(account.Id);

            return accountId;
        }

        public async Task<AccountDto?> GetAccountByIdAsync(Guid accountId, CancellationToken cancellationToken)
        {
            var cacheKey = $"account:{accountId}";
            Log.Information("Fetching account from cache. Key: {CacheKey}", cacheKey);

            var cached = await _cache.GetStringAsync(cacheKey, cancellationToken);
            if (!string.IsNullOrEmpty(cached))
            {
                Log.Information("Cache hit for AccountId: {AccountId}", accountId);
                return JsonSerializer.Deserialize<AccountDto>(cached);
            }

            Log.Information("Cache miss for AccountId: {AccountId}. Fetching from DB...", accountId);
            var account = await _accountRepository.GetAccountByIdAsync(accountId, cancellationToken);
            if (account == null)
            {
                Log.Warning("Account not found in DB. AccountId: {AccountId}", accountId);
                return null;
            }

            var accountDto = new AccountDto
            {
                Id = account.Id,
                AccountNumber = account.AccountNumber,
                Balance = account.Balance,
                CreatedAt = account.CreatedAt
            };

            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
            };

            var json = JsonSerializer.Serialize(accountDto);
            await _cache.SetStringAsync(cacheKey, json, options, cancellationToken);

            Log.Information("Account data cached after DB retrieval. AccountId: {AccountId}", accountId);

            return accountDto;
        }

        public async Task<Account?> GetAccountByNumberAsync(string accountNumber)
        {
            Log.Information("Fetching account by AccountNumber: {AccountNumber}", accountNumber);
            return await _accountRepository.GetAccountByNumberAsync(accountNumber);
        }

        private async Task InvalidateAccountCache(Guid accountId)
        {
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var db = _redis.GetDatabase();

            var pattern = $"account:{accountId}*";
            var keys = server.Keys(pattern: pattern).ToArray();

            foreach (var key in keys)
            {
                await db.KeyDeleteAsync(key);
                Log.Information("Deleted stale cache key: {Key}", key);
            }

            if (keys.Length == 0)
            {
                Log.Information("No stale cache keys found for AccountId: {AccountId}", accountId);
            }
        }
    }
}
