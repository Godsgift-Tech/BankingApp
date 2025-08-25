using BankingApp.Core.Entities;
using BankingAPP.Applications.Features.Common.Interfaces;
using BankingAPP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BankingAPP.Infrastructure.Repositories
{
    public class AccountRepository : IAccountRepository
    {
        private readonly BankingDbContext _context;

        public AccountRepository(BankingDbContext context)
        {
            _context = context;
        }

        public async Task<Account?> GetAccountByIdWithTransactionsAsync(Guid accountId, CancellationToken cancellationToken)
        {
            return await _context.Accounts
                .Include(a => a.Transactions)
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == accountId, cancellationToken);
        }

        public async Task<Account?> GetByIdAsync(Guid accountId, CancellationToken cancellationToken)
        {
            return await _context.Accounts
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == accountId, cancellationToken);
        }

        public async Task<IEnumerable<Account>> GetAllAsync(CancellationToken cancellationToken)
        {
            return await _context.Accounts
                .Include(a => a.User)
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(Account account, CancellationToken cancellationToken)
        {
            await _context.Accounts.AddAsync(account, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(Account account, CancellationToken cancellationToken)
        {
            _context.Accounts.Update(account);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(Account account, CancellationToken cancellationToken)
        {
            _context.Accounts.Remove(account);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<Account?> GetByUserAndTypeAsync(string userId, string accountType)
        {
            return await _context.Accounts
                .FirstOrDefaultAsync(a => a.UserId == userId && a.AccountType == accountType);
        }

    }
}
