using BankingApp.Core.Enums;

namespace BankingApp.Application.DTO.Transactions
{
    public class TransactionDto
    {
        public Guid Id { get; set; }
        public TransactionType Type { get; set; }
        public decimal Amount { get; set; }
        public DateTime Timestamp { get; set; }
        public string? Description { get; set; }
        public string? TargetAccountNumber { get; set; }
        public TransactionStatus Status { get; set; }
        public decimal BalanceAfterTransaction { get; set; }
    }

}
