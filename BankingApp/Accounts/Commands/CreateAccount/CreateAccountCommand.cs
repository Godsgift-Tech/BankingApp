using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingApp.Application.Accounts.Commands.CreateAccount
{
    public class CreateAccountCommand : IRequest<Guid> // Returns the Account ID
    {
        public string UserId { get; set; } = default!;
        public string AccountNumber { get; set; } = default!;
    }

    
}
