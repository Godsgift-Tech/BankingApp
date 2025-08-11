using BankingApp.Core.Enums;
using System.Text.Json.Serialization;

namespace BankingApp.Core.Entities
{
    public class Transaction
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid AccountId { get; set; }
        [JsonIgnore]
        public Account Account { get; set; }

        public TransactionType Type { get; set; }
        public decimal Amount { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public string? Description { get; set; }

        // For transfer of funds to another account
        public string? TargetAccountNumber { get; set; }

        public TransactionStatus Status { get; set; } = TransactionStatus.Success;

        //  balance after this transaction
        public decimal BalanceAfterTransaction { get; set; }
    }
}
