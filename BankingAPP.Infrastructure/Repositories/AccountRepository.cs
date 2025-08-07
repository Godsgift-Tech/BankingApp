using BankingApp.Application.Interfaces.Repository;
using BankingApp.Core.Entities;
using BankingAPP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingAPP.Infrastructure.Repositories
{
    public class AccountRepository : IAccountRepository
    {
        private readonly BankingDbContext _context;

        public AccountRepository(BankingDbContext context)
        {
            _context = context;
        }

        public async Task<bool> AccountNumberExistsAsync(string accountNumber, CancellationToken cancellationToken)
        {
            return await _context.Accounts.AnyAsync(a => a.AccountNumber == accountNumber, cancellationToken);
        }

        public async Task<Guid> CreateAccountAsync(Account account, CancellationToken cancellationToken)
        {
            _context.Accounts.Add(account);
            await _context.SaveChangesAsync(cancellationToken);
            return account.Id;
        }

        public async Task<Account?> GetAccountByIdAsync(Guid accountId, CancellationToken cancellationToken)
        {
            return await _context.Accounts
                .Include(a => a.Transactions)
                .FirstOrDefaultAsync(a => a.Id == accountId, cancellationToken);
        }

        public async Task<Account?> GetAccountByNumberAsync(string accountNumber)
        {
            return await _context.Accounts.FirstOrDefaultAsync(a => a.AccountNumber == accountNumber);
        }

    }

}
