using BankingAPP.Applications.Features.Accounts.DTO;
using MediatR;
using PagedList;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingAPP.Applications.Features.Accounts.Queries.GetAllAcounts
{
    public class GetAllAccountsQuery : IRequest<IPagedList<AccountDto>>
    {
        public int PageNumber { get; set; } = 1;  
        public int PageSize { get; set; } = 10;   
    }
}
