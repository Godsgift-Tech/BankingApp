using BankingApp.Application.DTO.Accounts;
using BankingApp.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingApp.Application.Interfaces.Services
{

    public interface IAccountService
    {
        Task<CreateAccountResponseDto> CreateAccountAsync(string userId, CreateAccountDto dto, CancellationToken cancellationToken);
        Task<AccountDto?> GetAccountByIdAsync(Guid accountId, CancellationToken cancellationToken);
        Task<Account?> GetAccountByNumberAsync(string accountNumber, CancellationToken cancellationToken);
    }

}
