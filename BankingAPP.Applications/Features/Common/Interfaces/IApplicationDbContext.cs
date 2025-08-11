using BankingApp.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankingAPP.Applications.Features.Common.Interfaces
{
    public interface IApplicationDbContext
    {
        DbSet<Account> Accounts { get; }
        DbSet<Transaction> Transactions { get; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    }

}
