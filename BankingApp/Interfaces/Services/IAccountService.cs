using BankingApp.Application.DTO.Accounts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingApp.Application.Interfaces.Services
{
    public interface IAccountService
    {
        Task<Guid> CreateAccountAsync(CreateAccountDto dto, CancellationToken cancellationToken);
        Task<AccountDto?> GetAccountByIdAsync(Guid accountId, CancellationToken cancellationToken);
    }
}
