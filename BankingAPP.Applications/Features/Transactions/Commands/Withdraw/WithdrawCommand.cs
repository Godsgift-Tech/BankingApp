using BankingAPP.Applications.Features.Transactions.DTO;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingAPP.Applications.Features.Transactions.Commands.Withdraw
{
    public class WithdrawCommand : IRequest<TransactionHistoryDto>
    {
        public string AccountNumber { get; set; } = default!;

        public decimal Amount { get; set; }

        public string Description { get; set; } = "Withdrawal";
    }
}
