using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BankingApp.Core.Entities;


namespace BankingAPP.Applications.Features.Common.Interfaces
{
    public interface IAccountRepository
    {
        Task<Account?> GetAccountByIdWithTransactionsAsync(Guid accountId, CancellationToken cancellationToken);
        Task<Account?> GetByIdAsync(Guid accountId, CancellationToken cancellationToken);
        Task<IEnumerable<Account>> GetAllAsync(CancellationToken cancellationToken);
        Task AddAsync(Account account, CancellationToken cancellationToken);
        Task UpdateAsync(Account account, CancellationToken cancellationToken);
        Task DeleteAsync(Account account, CancellationToken cancellationToken);
    }
}
