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
        public Guid? AccountId { get; set; }  
        public string? AccountNumber { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public ExportFormat Format { get; set; } = ExportFormat.Pdf;
    }

}
