using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingAPP.Applications.Features.Accounts.DTO
{
    public class AccountDto
    {
        public Guid Id { get; set; }
        public string AccountNumber { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public string AccountType { get; set; } = default!; // e.g., Savings, Current
        public string Currency { get; set; } = "NGN";       // default to Naira
        public decimal Balance { get; set; }
        public DateTime CreatedAt { get; set; }

        // Optional: include user details
        public string UserId { get; set; } = default!;
        public string FullName { get; set; } = default!;

        // Optional: include transactions if needed
       // public List<TransactionDto> Transactions { get; set; } = new();
    }
}
