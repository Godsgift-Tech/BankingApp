using BankingApp.Core.Entities;
using BankingAPP.Applications.Features.Common.Interfaces;
using BankingAPP.Applications.Features.Transactions.DTO;
using BankingAPP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace BankingAPP.Infrastructure.Repositories
{
    public class TransactionRepository : ITransactionRepository
    {
        private readonly BankingDbContext _context;

        public TransactionRepository(BankingDbContext context)
        {
            _context = context;
        }

        public async Task<Transaction?> GetByIdAsync(Guid transactionId, CancellationToken cancellationToken)
        {
            return await _context.Transactions
                .Include(t => t.Account)
                .FirstOrDefaultAsync(t => t.Id == transactionId, cancellationToken);
        }

        public async Task<IEnumerable<Transaction>> GetByAccountIdAsync(Guid accountId, CancellationToken cancellationToken)
        {
            return await _context.Transactions
                .Where(t => t.AccountId == accountId)
                .OrderByDescending(t => t.Timestamp)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Transaction>> GetByAccountIdPagedAsync(Guid accountId, int pageNumber, int pageSize, CancellationToken cancellationToken)
        {
            return await _context.Transactions
                .Where(t => t.AccountId == accountId)
                .OrderByDescending(t => t.Timestamp)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Transaction>> GetByAccountIdPagedAsync(
            Guid accountId,
            int pageNumber,
            int pageSize,
            DateTime? fromDate,
            DateTime? toDate,
            CancellationToken cancellationToken)
        {
            var query = _context.Transactions.AsQueryable()
                .Where(t => t.AccountId == accountId);

            if (fromDate.HasValue)
                query = query.Where(t => t.Timestamp >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(t => t.Timestamp <= toDate.Value);

            return await query
                .OrderByDescending(t => t.Timestamp)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(Transaction transaction, CancellationToken cancellationToken)
        {
            await _context.Transactions.AddAsync(transaction, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(Transaction transaction, CancellationToken cancellationToken)
        {
            _context.Transactions.Update(transaction);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<List<TransactionHistoryDto>> GetTransactionsAsync(
            Guid accountId,
            DateTime? fromDate,
            DateTime? toDate,
            CancellationToken cancellationToken)
        {
            Log.Information("Fetching transactions for AccountId {AccountId}", accountId);

            var query = _context.Transactions
                .Where(t => t.AccountId == accountId);

            if (fromDate.HasValue)
                query = query.Where(t => t.Timestamp >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(t => t.Timestamp <= toDate.Value);

            return await query
                .OrderByDescending(t => t.Timestamp)
                .Select(t => new TransactionHistoryDto
                {
                    Timestamp = t.Timestamp,
                    Description = t.Description,
                    Amount = t.Amount,
                    BalanceAfterTransaction = t.BalanceAfterTransaction
                })
                .ToListAsync(cancellationToken);
        }

        public async Task DeleteAsync(Transaction transaction, CancellationToken cancellationToken)
        {
            _context.Transactions.Remove(transaction);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
