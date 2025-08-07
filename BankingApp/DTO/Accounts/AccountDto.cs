namespace BankingApp.Application.DTO.Accounts
{
    public class AccountDto
    {
        public Guid Id { get; set; }
        public string AccountNumber { get; set; } = default!;
        public decimal Balance { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
