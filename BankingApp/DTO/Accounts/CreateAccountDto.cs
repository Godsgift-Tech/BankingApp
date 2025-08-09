using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingApp.Application.DTO.Accounts
{
    public class CreateAccountDto
    {
        public string AccountType { get; set; } = default!;
        public string Currency { get; set; } = "NGN"; // default to Naira
    }
}
