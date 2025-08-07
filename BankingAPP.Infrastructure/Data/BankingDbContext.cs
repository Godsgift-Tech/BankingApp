using BankingApp.Core.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BankingAPP.Infrastructure.Data
{
    public class BankingDbContext : IdentityDbContext<ApplicationUser>
    {
        public BankingDbContext(DbContextOptions<BankingDbContext> options)
            : base(options) { }

        public DbSet<Account> Accounts { get; set; }
        public DbSet<Transaction> Transactions { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Account Configuration
            builder.Entity<Account>()
                .HasIndex(a => a.AccountNumber)
                .IsUnique();

            builder.Entity<Account>()
                .HasOne(a => a.User)
                .WithMany(u => u.Accounts)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Transaction Configuration
            builder.Entity<Transaction>()
                .HasOne(t => t.Account)
                .WithMany(a => a.Transactions)
                .HasForeignKey(t => t.AccountId)
                .OnDelete(DeleteBehavior.Cascade);

            // Optional: Enum conversion for TransactionType and Status (if using EF Core 5+)
            builder.Entity<Transaction>()
                .Property(t => t.Type)
                .HasConversion<string>(); // saves enum as string

            builder.Entity<Transaction>()
                .Property(t => t.Status)
                .HasConversion<string>(); // saves enum as string
        }
    }
}
