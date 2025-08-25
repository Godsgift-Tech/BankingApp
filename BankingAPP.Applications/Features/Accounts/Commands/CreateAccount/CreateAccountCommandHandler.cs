using BankingApp.Core.Entities;
using BankingAPP.Applications.Features.Accounts.DTO;
using BankingAPP.Applications.Features.Common.Exceptions;
using BankingAPP.Applications.Features.Common.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace BankingAPP.Applications.Features.Accounts.Commands.CreateAccount
{
    public class CreateAccountCommandHandler : IRequestHandler<CreateAccountCommand, AccountDto>
    {
        private readonly IAccountRepository _accountRepository;
        private readonly IUserRepository _userRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CreateAccountCommandHandler(
            IAccountRepository accountRepository,
            IUserRepository userRepository,
            IHttpContextAccessor httpContextAccessor)
        {
            _accountRepository = accountRepository;
            _userRepository = userRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<AccountDto> Handle(CreateAccountCommand request, CancellationToken cancellationToken)
        {
            var userId = _httpContextAccessor.HttpContext?.User
                .FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("User is not authenticated.");

            //  Check if user exists
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new KeyNotFoundException($"User with ID {userId} not found.");

            //  Check if user already has an account of this type
            var existingAccount = await _accountRepository.GetByUserAndTypeAsync(userId, request.AccountType);
            if (existingAccount != null)
                throw new ValidationException(
     $"User already has a {request.AccountType} account and cannot create another of the same type."
 );


            //  Normalize account type before saving
            var normalizedType = request.AccountType.Trim().ToLower() == "savings"
                ? "Savings" : "Current";

            var account = new Account
            {
                UserId = userId,
                AccountNumber = GenerateAccountNumber(),
                AccountType = normalizedType,
                Currency = request.Currency,
                CreatedAt = DateTime.UtcNow
            };

            await _accountRepository.AddAsync(account, cancellationToken);

            return new AccountDto
            {
                Id = account.Id,
                AccountNumber = account.AccountNumber,
                AccountName = $"{user.FirstName} {user.LastName}",
                Balance = account.Balance,
                CreatedAt = account.CreatedAt,
                UserId = account.UserId,
                FullName = $"{user.FirstName} {user.LastName}",
                AccountType = account.AccountType
            };
        }

        private string GenerateAccountNumber()
        {
            return new Random().Next(1000000000, int.MaxValue).ToString();
        }
    }
}
