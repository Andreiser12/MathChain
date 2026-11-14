using MathChain.Domain.Entities;
using MathChain.Domain.Gateways;
using MathChain.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace MathChain.Gateway.Repositories
{
    public class ExternalLoginRepository : BaseRepository, IExternalLoginGateway
    {
        public ExternalLoginRepository(MathChainDbContext context) : base(context)
        {
        }

        public async Task<ExternalLogin?> GetAsync(string provider, string subject)
        {
            return await _context.ExternalLogins
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.Provider == provider && l.Subject == subject);
        }

        public async Task<bool> TryCreateUserWithLoginAsync(User user, ExternalLogin login)
        {
            _context.Users.Add(user);
            _context.ExternalLogins.Add(login);

            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException)
            {
                _context.ChangeTracker.Clear();

                var alreadyRegistered = await _context.ExternalLogins
                    .AsNoTracking()
                    .AnyAsync(l => l.Provider == login.Provider && l.Subject == login.Subject);

                if (!alreadyRegistered)
                {
                    throw;
                }

                return false;
            }
        }
    }
}
