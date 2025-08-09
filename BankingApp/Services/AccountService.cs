using BankingApp.Application.DTO.Accounts;
using BankingApp.Application.Interfaces.Repository;
using BankingApp.Application.Interfaces.Services;
using BankingApp.Core.Entities;
using Serilog;

namespace BankingApp.Application.Services
{
    public class AccountService : IAccountService
    {
        private readonly IAccountRepository _accountRepository;
        private readonly IUserRepository _userRepository;

        public AccountService(IAccountRepository accountRepository, IUserRepository userRepository)
        {
            _accountRepository = accountRepository;
            _userRepository = userRepository;
        }

        public async Task<CreateAccountResponseDto> CreateAccountAsync(string userId, CreateAccountDto dto, CancellationToken cancellationToken)
        {
            // Fetch the user so we can return their name
            var user = await _userRepository.GetUserByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                Log.Warning("User not found. UserId: {UserId}", userId);
                throw new InvalidOperationException("User does not exist");
            }

            // Generate unique account number
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
                AccountType = account.AccountType,
                Currency = account.Currency,
                CreatedAt = account.CreatedAt,
                UserId = account.UserId,
                FullName = account.User != null
                    ? $"{account.User.FirstName} {account.User.LastName}"
                    : string.Empty
            };
        }

        public async Task<Account?> GetAccountByNumberAsync(string accountNumber, CancellationToken cancellationToken)
        {
            Log.Information("Fetching account by AccountNumber: {AccountNumber}", accountNumber);
            return await _accountRepository.GetAccountByNumberAsync(accountNumber, cancellationToken);
        }
    }
}
