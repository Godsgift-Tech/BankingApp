using BankingApp.Application.DTO.Accounts;
using BankingApp.Application.DTO.Transactions;
using BankingApp.Application.Interfaces.Repository;
using BankingApp.Application.Interfaces.Services;
using BankingApp.Core.Entities;
using Microsoft.Extensions.Caching.Distributed;
using Serilog;
using System.Text.Json;

namespace BankingApp.Application.Services
{
    public class AccountService : IAccountService
    {
        private readonly IAccountRepository _accountRepository;
        private readonly IUserRepository _userRepository;
        private readonly IDistributedCache _cache;

        public AccountService(
            IAccountRepository accountRepository,
            IUserRepository userRepository,
            IDistributedCache cache)
        {
            _accountRepository = accountRepository;
            _userRepository = userRepository;
            _cache = cache;
        }

        public async Task<CreateAccountResponseDto> CreateAccountAsync(string userId, CreateAccountDto dto, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetUserByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                Log.Warning("User not found. UserId: {UserId}", userId);
                throw new InvalidOperationException("User does not exist");
            }

            string accountNumber = await GenerateUniqueAccountNumberAsync(cancellationToken);

            Log.Information("Creating account for UserId: {UserId} with AccountNumber: {AccountNumber}", userId, accountNumber);

            var account = new Account
            {
                UserId = userId,
                AccountNumber = accountNumber,
                CreatedAt = DateTime.UtcNow,
                Balance = 0,
                AccountType = dto.AccountType,
                Currency = dto.Currency
            };

            var accountId = await _accountRepository.CreateAccountAsync(account, cancellationToken);

            // Store account in Redis
            await _cache.SetStringAsync(
                $"account:{accountId}",
                JsonSerializer.Serialize(account),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                },
                cancellationToken
            );

            Log.Information("Account created successfully. AccountId: {AccountId}", accountId);

            return new CreateAccountResponseDto
            {
                AccountId = accountId,
                AccountNumber = accountNumber,
                Name = $"{user.FirstName} {user.LastName}",
                CreatedAt = account.CreatedAt
            };
        }

        private async Task<string> GenerateUniqueAccountNumberAsync(CancellationToken cancellationToken)
        {
            var random = new Random();
            string accountNumber;

            do
            {
                accountNumber = random.Next(1000000000, int.MaxValue)
                                       .ToString()
                                       .Substring(0, 10);
            }
            while (await _accountRepository.AccountNumberExistsAsync(accountNumber, cancellationToken));

            return accountNumber;
        }

        public async Task<AccountDto?> GetAccountByIdAsync(Guid accountId, CancellationToken cancellationToken)
        {
            // Using Redis cache first
            var cachedAccount = await _cache.GetStringAsync($"account:{accountId}", cancellationToken);
            if (!string.IsNullOrEmpty(cachedAccount))
            {
                Log.Information("Returning account from Redis cache. AccountId: {AccountId}", accountId);
                return JsonSerializer.Deserialize<AccountDto>(cachedAccount);
            }

            //  Fetching from database with transaction details
            Log.Information("Fetching account from DB. AccountId: {AccountId}", accountId);
            var dbAccount = await _accountRepository.GetAccountByIdWithTransactionsAsync(accountId, cancellationToken);
            if (dbAccount == null)
            {
                Log.Warning("Account not found. AccountId: {AccountId}", accountId);
                return null;
            }

            //  Mapping entity to  DTO 
            var accountDto = new AccountDto
            {
                Id = dbAccount.Id,
                AccountNumber = dbAccount.AccountNumber,
                Balance = dbAccount.Balance,
                AccountType = dbAccount.AccountType,
                Currency = dbAccount.Currency,
                CreatedAt = dbAccount.CreatedAt,
                UserId = dbAccount.UserId,
                FullName = $"{dbAccount.User.FirstName} {dbAccount.User.LastName}",
                Transactions = dbAccount.Transactions
                    .OrderByDescending(t => t.Timestamp) //  newest first or on top
                    .Select(t => new TransactionDto
                    {
                        Id = t.Id,
                        Type = t.Type,
                        Amount = t.Amount,
                        Timestamp = t.Timestamp,
                        Description = t.Description,
                        TargetAccountNumber = t.TargetAccountNumber,
                        Status = t.Status,
                        BalanceAfterTransaction = t.BalanceAfterTransaction
                    })
                    .ToList()
            };

            //  Cache the DTO safely 
            await _cache.SetStringAsync(
                $"account:{accountId}",
                JsonSerializer.Serialize(accountDto),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                },
                cancellationToken
            );

            return accountDto;
        }



        public async Task<Account?> GetAccountByNumberAsync(string accountNumber, CancellationToken cancellationToken)
        {
            // Try Redis first
            var cachedAccount = await _cache.GetStringAsync($"account:number:{accountNumber}", cancellationToken);
            if (!string.IsNullOrEmpty(cachedAccount))
            {
                Log.Information("Returning account from Redis cache. AccountNumber: {AccountNumber}", accountNumber);
                return JsonSerializer.Deserialize<Account>(cachedAccount);
            }

            Log.Information("Fetching account by AccountNumber from DB: {AccountNumber}", accountNumber);
            var dbAccount = await _accountRepository.GetAccountByNumberAsync(accountNumber, cancellationToken);

            if (dbAccount != null)
            {
                await _cache.SetStringAsync(
                    $"account:number:{accountNumber}",
                    JsonSerializer.Serialize(dbAccount),
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                    },
                    cancellationToken
                );
            }

            return dbAccount;
        }

        private AccountDto MapToAccountDto(Account account)
        {
            return new AccountDto
            {
                Id = account.Id,
                AccountNumber = account.AccountNumber,
                Balance = account.Balance,
                AccountType = account.AccountType,
                Currency = account.Currency,
                CreatedAt = account.CreatedAt,
                UserId = account.UserId,
                FullName = account.User != null
                    ? $"{account.User.FirstName} {account.User.LastName}"
                    : string.Empty
            };
        }
    }
}
