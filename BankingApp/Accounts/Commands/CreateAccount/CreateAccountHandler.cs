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
        private readonly BankingDbContext _context;

        public CreateAccountHandler(BankingDbContext context)
        {
            _context = context;
        }

        public async Task<Guid> Handle(CreateAccountCommand request, CancellationToken cancellationToken)
        {
            // Check if account number already exists
            bool exists = await _context.Accounts
                .AnyAsync(a => a.AccountNumber == request.AccountNumber, cancellationToken);

            if (exists)
                throw new Exception("Account number already exists.");

            var account = new Account
            {
                AccountNumber = request.AccountNumber,
                UserId = request.UserId,
                Balance = 0m,
                CreatedAt = DateTime.UtcNow
            };

            _context.Accounts.Add(account);
            await _context.SaveChangesAsync(cancellationToken);

            return account.Id;
        }
    }


}
