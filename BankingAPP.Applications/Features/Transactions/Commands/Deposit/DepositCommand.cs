using BankingAPP.Applications.Features.Transactions.DTO;
using MediatR;

namespace BankingAPP.Applications.Features.Transactions.Commands.Deposit
{
    public class DepositCommand : IRequest<TransactionHistoryDto>
    {
        public string AccountNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Description { get; set; } = "Deposit";
    }
}
