using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingAPP.Applications.Features.Accounts.Commands.DeleteAccount
{
    public record DeleteAccountCommand(Guid AccountId) : IRequest<Unit>;
}
