using BankingApp.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingApp.Application.DTO.Transactions
{
    public class TransactionHistoryDto
    {
        public Guid Id { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; } = default!;
        public DateTime Timestamp { get; set; }
        public string Type { get; set; } = default!; 
        public string Status { get; set; } = default!; 
        public string? TargetAccountNumber { get; set; }
        public decimal BalanceAfterTransaction { get; set; }
    }

}
