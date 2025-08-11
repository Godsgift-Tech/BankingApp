namespace BankingApp.Application.DTO.Accounts
{
    public class AccountDto
    {
        public Guid Id { get; set; }
        public string AccountNumber { get; set; } = default!;
        public decimal Balance { get; set; }
        public string AccountType { get; set; } = default!;
        public string Currency { get; set; } = default!;
        public DateTime CreatedAt { get; set; }

        // Optional: include user details
        public string UserId { get; set; } = default!;
        public string FullName { get; set; } = default!;
    }
}
