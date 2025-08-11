using BankingApp.Core.Entities;
using BankingAPP.Applications.Features.Accounts.DTO;
using BankingAPP.Applications.Features.Common.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

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
            var userId = _httpContextAccessor.HttpContext?.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("User is not authenticated.");

            // 1. Check if user exists
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new KeyNotFoundException($"User with ID {userId} not found.");

            // 2. Create account entity
            var account = new Account
            {
                UserId = userId,
                AccountNumber = GenerateAccountNumber(),
                AccountType = request.AccountType,
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
                FullName = $"{user.FirstName} {user.LastName}"
            };
        }

        private string GenerateAccountNumber()
        {
            return new Random().Next(1000000000, int.MaxValue).ToString();
        }
    }

}
