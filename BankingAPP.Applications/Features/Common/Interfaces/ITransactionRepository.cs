using BankingApp.Core.Entities;
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
        Task AddAsync(Transaction transaction, CancellationToken cancellationToken);
        Task UpdateAsync(Transaction transaction, CancellationToken cancellationToken);
        Task DeleteAsync(Transaction transaction, CancellationToken cancellationToken);
    }
}
