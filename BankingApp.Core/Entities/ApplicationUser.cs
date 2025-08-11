
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;

namespace BankingApp.Core.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = default!;
        public string LastName { get; set; } = default!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<Account> Accounts { get; set; } = new List<Account>();
    }
}
