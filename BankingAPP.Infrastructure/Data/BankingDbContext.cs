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
                .Property(a => a.Currency)
                .HasDefaultValue("NGN");

            builder.Entity<Account>()
                .Property(a => a.AccountType)
                .IsRequired();

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

            // Enum conversion for TransactionType and Status 
            builder.Entity<Transaction>()
                .Property(t => t.Type)
                .HasConversion<string>();

            builder.Entity<Transaction>()
                .Property(t => t.Status)
                .HasConversion<string>();

            //  BalanceAfterTransaction is required (non-nullable)
            builder.Entity<Transaction>()
                .Property(t => t.BalanceAfterTransaction)
                .IsRequired();
        }
    }
}
