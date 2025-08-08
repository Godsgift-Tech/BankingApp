using BankingApp.Application.DTO.Accounts;
using BankingApp.Application.Interfaces.Repository;
using BankingApp.Application.Interfaces.Services;
using BankingApp.Core.Entities;
using Serilog;
using System.Text.Json;

namespace BankingApp.Application.Services
{
    public class AccountService : IAccountService
    {
        private readonly IAccountRepository _accountRepository;

        public AccountService(IAccountRepository accountRepository)
        {
            _accountRepository = accountRepository;
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

            Log.Information("Account created successfully. AccountId: {AccountId}", accountId);

            // No Redis cache to invalidate
            Log.Information("Cache invalidation skipped (Redis removed).");

            return accountId;
        }

        public async Task<AccountDto?> GetAccountByIdAsync(Guid accountId, CancellationToken cancellationToken)
        {
            Log.Information("Fetching account from DB. AccountId: {AccountId}", accountId);

            var account = await _accountRepository.GetAccountByIdAsync(accountId, cancellationToken);
            if (account == null)
            {
                Log.Warning("Account not found. AccountId: {AccountId}", accountId);
                return null;
            }

            return new AccountDto
            {
                Id = account.Id,
                AccountNumber = account.AccountNumber,
                Balance = account.Balance,
                CreatedAt = account.CreatedAt
            };
        }

        public async Task<Account?> GetAccountByNumberAsync(string accountNumber)
        {
            Log.Information("Fetching account by AccountNumber: {AccountNumber}", accountNumber);
            return await _accountRepository.GetAccountByNumberAsync(accountNumber);
        }
    }
}
