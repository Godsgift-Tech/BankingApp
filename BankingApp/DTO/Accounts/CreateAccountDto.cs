using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingApp.Application.DTO.Accounts
{
    public class CreateAccountDto
    {
        public string UserId { get; set; } = default!;
        public string AccountNumber { get; set; } = default!;
    }
}
