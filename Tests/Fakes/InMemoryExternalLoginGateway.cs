using MathChain.Domain.Entities;
using MathChain.Domain.Gateways;

namespace MathChain.Tests.Fakes
{
    public class InMemoryExternalLoginGateway : IExternalLoginGateway
    {
        private readonly InMemoryUserGateway _users;

        public InMemoryExternalLoginGateway(InMemoryUserGateway users)
        {
            _users = users;
        }

        public List<ExternalLogin> Logins { get; } = new();
        public (User User, ExternalLogin Login)? ConcurrentRegistration { get; set; }

        public Task<ExternalLogin?> GetAsync(string provider, string subject)
        {
            return Task.FromResult(EntityCopy.Detached(Logins.FirstOrDefault(l => l.Provider == provider && l.Subject == subject)));
        }

        public Task<bool> TryCreateUserWithLoginAsync(User user, ExternalLogin login)
        {
            if (ConcurrentRegistration != null)
            {
                _users.Users.Add(ConcurrentRegistration.Value.User);
                Logins.Add(ConcurrentRegistration.Value.Login);
                ConcurrentRegistration = null;
            }

            if (Logins.Any(l => l.Provider == login.Provider && l.Subject == login.Subject))
            {
                return Task.FromResult(false);
            }

            _users.Users.Add(user);
            Logins.Add(login);
            return Task.FromResult(true);
        }
    }
}
