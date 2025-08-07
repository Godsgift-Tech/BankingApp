namespace BankingApp.Application.DTO.Transactions
{
    public class TransferDto
    {
        public Guid FromAccountId { get; set; }
        public string ToAccountNumber { get; set; } = default!;
        public decimal Amount { get; set; }
        public string Description { get; set; } = "Transfer";
    }
}
