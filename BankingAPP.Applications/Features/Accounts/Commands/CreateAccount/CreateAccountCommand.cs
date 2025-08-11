using BankingAPP.Applications.Features.Accounts.DTO;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingAPP.Applications.Features.Accounts.Commands.CreateAccount
{
    public class CreateAccountCommand : IRequest<AccountDto>
    {
      //  public string UserId { get; set; } = default!;
        public string AccountType { get; set; } = default!;
        public string Currency { get; set; } = "NGN";
        //public decimal InitialBalance { get; set; }
    }
}
