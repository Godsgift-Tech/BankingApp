using BankingAPP.Applications.Features.Accounts.DTO;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingAPP.Applications.Features.Accounts.Commands.UpdateAccount
{
    public class UpdateAccountCommand : IRequest<AccountDto>
    {
        public Guid AccountId { get; set; }
        public string AccountType { get; set; } = default!;
        public string Currency { get; set; } = default!;
    }
}
