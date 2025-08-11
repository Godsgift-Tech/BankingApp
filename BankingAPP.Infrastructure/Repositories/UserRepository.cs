using BankingApp.Core.Entities;
using BankingAPP.Applications.Features.Common.Interfaces;
using BankingAPP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BankingAPP.Infrastructure.Repositories
{

    public class UserRepository : IUserRepository
    {
        private readonly BankingDbContext _context;

        public UserRepository(BankingDbContext context)
        {
            _context = context;
        }

        public async Task<ApplicationUser?> GetByIdAsync(string userId)
        {
            return await _context.Users
                .Include(u => u.Accounts)
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<ApplicationUser?> GetUserByIdAsync(string userId, CancellationToken cancellationToken)
        {
            return await _context.Users
                .Include(u => u.Accounts)
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        }

        public async Task<IEnumerable<ApplicationUser>> GetAllAsync()
        {
            return await _context.Users
                .Include(u => u.Accounts)
                .ToListAsync();
        }

        public async Task AddAsync(ApplicationUser user)
        {
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(ApplicationUser user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(string userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }
        }
    }



}
