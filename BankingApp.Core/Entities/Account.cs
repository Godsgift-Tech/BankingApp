namespace BankingApp.Core.Entities
{
    public class Account
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string UserId { get; set; } = default!;
        public ApplicationUser User { get; set; } = default!;

        public string AccountNumber { get; set; } = default!;
        public decimal Balance { get; set; } = 0m;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }


   


}
