using BankingAPP.Applications.Features.Transactions.DTO;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingAPP.Applications.Features.Transactions.Queries.GetTransactionHistory
{
    public class GetTransactionHistoryQuery : IRequest<IEnumerable<TransactionHistoryDto>>
    {
        public Guid AccountId { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        //  date filters
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
