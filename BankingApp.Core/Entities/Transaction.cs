

using BankingApp.Core.Enums;

namespace BankingApp.Core.Entities
{
    public class Transaction
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid AccountId { get; set; }
        public Account Account { get; set; } = default!;

        public TransactionType Type { get; set; }
        public decimal Amount { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public string? Description { get; set; }

        // Optional: For transfers
        public string? TargetAccountNumber { get; set; }

        public TransactionStatus Status { get; set; } = TransactionStatus.Success;
    }


   


}
