using System;
using System.Text.Json.Serialization;

namespace BankingAPP.Applications.Features.Transactions.DTO
{
    public class TransactionHistoryDto
    {
        [JsonIgnore]
        public Guid Id { get; set; }

        public string AccountNumber { get; set; } = string.Empty;

        public string Currency { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        //  returns value with currency
        public string AmountWithCurrency => $"{Currency} {Amount:N2}";

        public string Description { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; }

        public string Type { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public string? TargetAccountNumber { get; set; }

        public decimal BalanceAfterTransaction { get; set; }

        // always returns value with currency
        public string BalanceAfterTransactionWithCurrency => $"{Currency} {BalanceAfterTransaction:N2}";
    }
}
