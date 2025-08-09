using BankingApp.Core.Entities;

namespace BankingApp.Application.Interfaces.Repository
{
    public interface IUserRepository
    {
        
        Task<ApplicationUser?> GetUserByIdAsync(string userId, CancellationToken cancellationToken);
    }
}
