using MathChain.Domain.Entities;
using MathChain.Domain.Gateways;

namespace MathChain.Tests.Fakes
{
    public class InMemoryUserGateway : IUserGateway
    {
        public List<User> Users { get; } = new();

        public Task<User?> GetByWalletAddressAsync(string walletAddress)
        {
            return Task.FromResult(Users.FirstOrDefault(u => u.WalletAddress == walletAddress));
        }

        public Task<List<User>> GetAllAsync()
        {
            return Task.FromResult(Users.ToList());
        }

        public Task<User> CreateOrUpdateAsync(User user)
        {
            Users.Add(user);
            return Task.FromResult(user);
        }
    }
}
