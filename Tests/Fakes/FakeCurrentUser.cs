using MathChain.Domain.Enums;
using MathChain.Domain.Interfaces;

namespace MathChain.Tests.Fakes
{
    public class FakeCurrentUser : ICurrentUser
    {
        public string? WalletAddress { get; private set; }
        public UserRole? Role { get; private set; }
        public bool IsAuthenticated => WalletAddress != null;

        public FakeCurrentUser As(string walletAddress, UserRole role)
        {
            WalletAddress = walletAddress;
            Role = role;
            return this;
        }

        public FakeCurrentUser Anonymous()
        {
            WalletAddress = null;
            Role = null;
            return this;
        }
    }
}
