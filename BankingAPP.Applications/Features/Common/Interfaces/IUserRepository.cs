using BankingApp.Core.Entities;

namespace BankingAPP.Applications.Features.Common.Interfaces
{
    public interface IUserRepository
    {
        Task<ApplicationUser?> GetByIdAsync(string userId);
        Task<ApplicationUser?> GetUserByIdAsync(string userId, CancellationToken cancellationToken);
        Task<IEnumerable<ApplicationUser>> GetAllAsync();
        Task AddAsync(ApplicationUser user);
        Task UpdateAsync(ApplicationUser user);
        Task DeleteAsync(string userId);
    }
}
