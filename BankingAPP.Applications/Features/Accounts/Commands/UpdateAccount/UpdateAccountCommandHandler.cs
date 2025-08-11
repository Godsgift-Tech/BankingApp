using BankingAPP.Applications.Features.Accounts.DTO;
using BankingAPP.Applications.Features.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingAPP.Applications.Features.Accounts.Commands.UpdateAccount
{
    public class UpdateAccountCommandHandler : IRequestHandler<UpdateAccountCommand, AccountDto>
    {
        private readonly IAccountRepository _accountRepository;
        private readonly IDistributedCache _cache;

        public UpdateAccountCommandHandler(IAccountRepository accountRepository, IDistributedCache cache)
        {
            _accountRepository = accountRepository;
            _cache = cache;
        }

        public async Task<AccountDto> Handle(UpdateAccountCommand request, CancellationToken cancellationToken)
        {
            var account = await _accountRepository.GetByIdAsync(request.AccountId, cancellationToken);
            if (account == null)
                throw new KeyNotFoundException($"Account with ID {request.AccountId} not found.");

            account.AccountType = request.AccountType;
            account.Currency = request.Currency;

            await _accountRepository.UpdateAsync(account, cancellationToken);

            // ❌ Invalidate cache so fresh data will be loaded next time
            string cacheKey = $"account:{account.Id}";
            await _cache.RemoveAsync(cacheKey, cancellationToken);

            return new AccountDto
            {
                Id = account.Id,
                AccountNumber = account.AccountNumber,
                Balance = account.Balance,
                AccountType = account.AccountType,
                Currency = account.Currency,
                CreatedAt = account.CreatedAt,
                UserId = account.UserId,
                FullName = $"{account.User?.FirstName} {account.User?.LastName}".Trim()
            };
        }
    }

}
