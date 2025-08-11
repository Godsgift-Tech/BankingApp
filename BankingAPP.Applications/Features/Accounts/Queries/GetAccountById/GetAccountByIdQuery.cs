using BankingAPP.Applications.Features.Accounts.DTO;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingAPP.Applications.Features.Accounts.Queries.GetAccountById
{
    public record GetAccountByIdQuery(Guid AccountId) : IRequest<AccountDto?>;
}
