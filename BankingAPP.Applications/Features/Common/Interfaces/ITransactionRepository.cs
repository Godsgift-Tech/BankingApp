using BankingApp.Core.Entities;
using BankingAPP.Applications.Features.Transactions.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingAPP.Applications.Features.Common.Interfaces
{
    public interface ITransactionRepository
    {
        Task<Transaction?> GetByIdAsync(Guid transactionId, CancellationToken cancellationToken);
        Task<IEnumerable<Transaction>> GetByAccountIdAsync(Guid accountId, CancellationToken cancellationToken);
        Task<IEnumerable<Transaction>> GetByAccountIdPagedAsync(Guid accountId, int pageNumber, int pageSize, CancellationToken cancellationToken);

        //  Paged + Date Range
        Task<IEnumerable<Transaction>> GetByAccountIdPagedAsync(
            Guid accountId,
            int pageNumber,
            int pageSize,
            DateTime? fromDate,
            DateTime? toDate,
            CancellationToken cancellationToken);
        Task<List<TransactionHistoryDto>> GetTransactionsAsync(
           Guid accountId,
           DateTime? fromDate,
           DateTime? toDate,
           CancellationToken cancellationToken);

        Task AddAsync(Transaction transaction, CancellationToken cancellationToken);
        Task UpdateAsync(Transaction transaction, CancellationToken cancellationToken);
        Task DeleteAsync(Transaction transaction, CancellationToken cancellationToken);
    }

}
