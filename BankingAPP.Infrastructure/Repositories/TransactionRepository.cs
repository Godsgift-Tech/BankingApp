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

    public class TransactionRepository : ITransactionRepository
    {
        private readonly BankingDbContext _context;

        public TransactionRepository(BankingDbContext context)
        {
            _context = context;
        }

        public async Task<Account?> GetAccountByIdAsync(Guid accountId)
        {
            return await _context.Accounts.FindAsync(accountId);
        }

        public async Task<Account?> GetAccountByNumberAsync(string accountNumber)
        {
            return await _context.Accounts.FirstOrDefaultAsync(a => a.AccountNumber == accountNumber);
        }

        public async Task UpdateAccountAsync(Account account)
        {
            _context.Accounts.Update(account);
            // No SaveChangesAsync here, it will be called autmoatically not individually.
        }

        public async Task AddTransactionAsync(Transaction transaction)
        {
            _context.Transactions.Add(transaction);
            // No SaveChangesAsync here, it will be called autmoatically not individually.


        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<(List<Transaction> Transactions, int TotalCount)> GetPagedTransactionsByAccountIdAsync(
     Guid accountId,
     int page,
     int pageSize,
     DateTime? fromDate,
     DateTime? toDate,
     CancellationToken cancellationToken)
        {
            var query = _context.Transactions
                .Where(t => t.AccountId == accountId);

            if (fromDate.HasValue)
                query = query.Where(t => t.Timestamp >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(t => t.Timestamp <= toDate.Value);

            query = query.OrderByDescending(t => t.Timestamp);

            var totalCount = await query.CountAsync(cancellationToken);

            var transactions = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (transactions, totalCount);
        }


    }


}
