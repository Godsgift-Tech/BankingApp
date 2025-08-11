using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingAPP.Applications.Features.Transactions.Queries.ExportTransactions
{
    public class ExportTransactionsQuery : IRequest<ExportTransactionsResultDto>
    {
        public Guid AccountId { get; set; }
        public string AccountNumber { get; set; } = string.Empty; // Added AccountNumber
        public DateTime? FromDate { get; set; }   // optional filter
        public DateTime? ToDate { get; set; }     // optional filter
        public ExportFormat Format { get; set; }  // CSV, Excel, PDF
    }
}
