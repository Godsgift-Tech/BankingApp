using BankingApp.Application.Interfaces.Repository;
using BankingApp.Core.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingApp.Application.Accounts.Commands.CreateAccount
{

    public class CreateAccountHandler : IRequestHandler<CreateAccountCommand, Guid>
    {
        private readonly IAccountRepository _repository;

        public CreateAccountHandler(IAccountRepository repository)
        {
            _repository = repository;
        }

        public async Task<Guid> Handle(CreateAccountCommand request, CancellationToken cancellationToken)
        {
            if (await _repository.AccountNumberExistsAsync(request.AccountNumber, cancellationToken))
                throw new Exception("Account number already exists.");

            var account = new Account
            {
                AccountNumber = request.AccountNumber,
                UserId = request.UserId,
                Balance = 0m,
                CreatedAt = DateTime.UtcNow
            };

            return await _repository.CreateAccountAsync(account, cancellationToken);
        }
    }



}
