using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingAPP.Applications.Features.Transactions.Commands.Transfer
{
    public class TransferCommand : IRequest<bool>
    {
        public Guid FromAccountId { get; set; }
        public string ToAccountNumber { get; set; } = default!;
        public decimal Amount { get; set; }
        public string Description { get; set; } = "Transfer";
    }
}
