using BankingAPP.Applications.Features.Transactions.DTO;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingAPP.Applications.Features.Transactions.Commands.Deposit
{
    public class DepositCommand : IRequest<TransactionHistoryDto>
    {
        public Guid AccountId { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; } = "Deposit";
    }
}
