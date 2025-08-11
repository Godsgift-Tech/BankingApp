namespace BankingAPP.Applications.Features.Transactions.DTO
{
    public class DepositDto
    {
        public Guid AccountId { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; } = "Deposit";
    }

}
