using BankingApp.Application.Interfaces.Repository;
using BankingApp.Core.Entities;
using BankingAPP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingAPP.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly BankingDbContext _context;

        public UserRepository(BankingDbContext context)
        {
            _context = context;
        }

        public async Task<ApplicationUser?> GetUserByIdAsync(string userId, CancellationToken cancellationToken)
        {
            return await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        }
    }


}
