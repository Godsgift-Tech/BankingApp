namespace BankingAPP.Applications.Features.Transactions.DTO
{
    public class WithdrawDto
    {
        public Guid AccountId { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; } = "Withdrawal";
    }

}
