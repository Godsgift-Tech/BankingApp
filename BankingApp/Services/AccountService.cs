using BankingApp.Application.DTO.Accounts;
using BankingApp.Application.Interfaces.Repository;
using BankingApp.Application.Interfaces.Services;
using BankingApp.Core.Entities;
using Microsoft.Extensions.Caching.Distributed;
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
            if (await _accountRepository.AccountNumberExistsAsync(dto.AccountNumber, cancellationToken))
                throw new Exception("Account number already exists.");

            var account = new Account
            {
                UserId = dto.UserId,
                AccountNumber = dto.AccountNumber,
                CreatedAt = DateTime.UtcNow
            };

            var accountId = await _accountRepository.CreateAccountAsync(account, cancellationToken);

            // Prepare cache DTO
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

            // Invalidate older keys if needed (e.g., if cache lists exist)
            await InvalidateAccountCache(account.Id);

            return accountId;
        }

        public async Task<AccountDto?> GetAccountByIdAsync(Guid accountId, CancellationToken cancellationToken)
        {
            var cacheKey = $"account:{accountId}";
            var cached = await _cache.GetStringAsync(cacheKey, cancellationToken);

            if (!string.IsNullOrEmpty(cached))
            {
                return JsonSerializer.Deserialize<AccountDto>(cached);
            }

            var account = await _accountRepository.GetAccountByIdAsync(accountId, cancellationToken);
            if (account == null)
                return null;

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

            return accountDto;
        }

        public async Task<Account?> GetAccountByNumberAsync(string accountNumber)
        {
            return await _accountRepository.GetAccountByNumberAsync(accountNumber);
        }

        // Delete old Redis keys (advanced cleanup)
        private async Task InvalidateAccountCache(Guid accountId)
        {
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var db = _redis.GetDatabase();

            var pattern = $"account:{accountId}*"; // Covers exact or related keys

            var keys = server.Keys(pattern: pattern).ToArray();

            foreach (var key in keys)
            {
                await db.KeyDeleteAsync(key);
            }
        }
    }
}
