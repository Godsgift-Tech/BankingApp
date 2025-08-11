using BankingApp.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingApp.Application.Interfaces.Repository
{

    public interface IAccountRepository
    {
        Task<Guid> CreateAccountAsync(Account account, CancellationToken cancellationToken);
        Task<bool> AccountNumberExistsAsync(string accountNumber, CancellationToken cancellationToken);
        Task<Account?> GetAccountByIdAsync(Guid accountId, CancellationToken cancellationToken);
        Task<Account?> GetAccountByNumberAsync(string accountNumber, CancellationToken cancellationToken);
    }



}
