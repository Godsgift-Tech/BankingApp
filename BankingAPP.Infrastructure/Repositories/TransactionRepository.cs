using BankingApp.Application.Interfaces.Repository;
using BankingApp.Core.Entities;
using BankingAPP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

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
            return await _context.Accounts
                .AsTracking() // Ensures EF Core tracks changes for update
                .FirstOrDefaultAsync(a => a.Id == accountId);
        }

        public async Task<Account?> GetAccountByNumberAsync(string accountNumber)
        {
            return await _context.Accounts
                .AsTracking()
                .FirstOrDefaultAsync(a => a.AccountNumber == accountNumber);
        }

        public Task UpdateAccountAsync(Account account)
        {
            _context.Accounts.Update(account);
            return Task.CompletedTask; // SaveChanges is handled in the service
        }

        public async Task AddTransactionAsync(Transaction transaction)
        {
            // Ensure timestamp is set if not already
            if (transaction.Timestamp == default)
                transaction.Timestamp = DateTime.UtcNow;

            // ✅ Ensure BalanceAfterTransaction is explicitly set
            transaction.BalanceAfterTransaction = Math.Round(transaction.BalanceAfterTransaction, 2);

            // ✅ Mark the property as modified so EF never ignores it
            _context.Entry(transaction).Property(t => t.BalanceAfterTransaction).IsModified = true;

            await _context.Transactions.AddAsync(transaction);
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
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 10 : pageSize;

            var query = _context.Transactions
                .Where(t => t.AccountId == accountId);

            if (fromDate.HasValue)
                query = query.Where(t => t.Timestamp >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(t => t.Timestamp <= toDate.Value);

            query = query.OrderByDescending(t => t.Timestamp);

            var totalCount = await query.CountAsync(cancellationToken);

            if (pageSize != int.MaxValue)
            {
                query = query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize);
            }

            var transactions = await query.ToListAsync(cancellationToken);

            return (transactions, totalCount);
        }

        public async Task<(List<Transaction> Transactions, int TotalCount)> GetPagedTransactionsByAccountNumberAsync(
      string accountNumber,
      int page,
      int pageSize,
      DateTime? fromDate,
      DateTime? toDate,
      CancellationToken cancellationToken)
        {
            var query = _context.Transactions
                .Include(t => t.Account)
                .Where(t => t.Account.AccountNumber == accountNumber);

            if (fromDate.HasValue)
                query = query.Where(t => t.Timestamp >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(t => t.Timestamp <= toDate.Value);

            var totalCount = await query.CountAsync(cancellationToken);

            var transactions = await query
                .OrderByDescending(t => t.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (transactions, totalCount);
        }


    }
}
