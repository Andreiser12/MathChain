using MathChain.Domain.Entities;
using MathChain.Domain.Gateways;
using MathChain.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathChain.Gateway.Repositories
{
    public class UserRepository : BaseRepository, IUserGateway
    {
        public UserRepository(MathChainDbContext context) : base(context)
        {

        }

        public async Task<User> CreateOrUpdateAsync(User user)
        {
            var wallet = user.WalletAddress.Trim().ToLower();

            var existing = await _context.Users
                .FirstOrDefaultAsync(u => u.WalletAddress.ToLower() == wallet);

            if(existing != null)
            {
                existing.Role = user.Role;
                await _context.SaveChangesAsync();
                return existing;
            }
            else
            {
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                return user;
            }
        }

        public async Task<List<User>> GetAllAsync()
        {
            return await _context.Users
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<User?> GetByWalletAddressAsync(string walletAddress)
        {
            if(string.IsNullOrWhiteSpace(walletAddress))
            {
                return null;
            }

            var wallet = walletAddress.Trim().ToLower();

            return await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.WalletAddress.ToLower() == wallet);
        }
    }
}
