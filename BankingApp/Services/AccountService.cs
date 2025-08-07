using BankingApp.Application.DTO.Accounts;
using BankingApp.Application.Interfaces.Repository;
using BankingApp.Application.Interfaces.Services;
using BankingApp.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            if (await _accountRepository.AccountNumberExistsAsync(dto.AccountNumber, cancellationToken))
                throw new Exception("Account number already exists.");

            var account = new Account
            {
                UserId = dto.UserId,
                AccountNumber = dto.AccountNumber,
                CreatedAt = DateTime.UtcNow
            };

            return await _accountRepository.CreateAccountAsync(account, cancellationToken);
        }

        public async Task<AccountDto?> GetAccountByIdAsync(Guid accountId, CancellationToken cancellationToken)
        {
            var account = await _accountRepository.GetAccountByIdAsync(accountId, cancellationToken);
            if (account == null)
                return null;

            return new AccountDto
            {
                Id = account.Id,
                AccountNumber = account.AccountNumber,
                Balance = account.Balance,
                CreatedAt = account.CreatedAt
            };
        }
    }


}
