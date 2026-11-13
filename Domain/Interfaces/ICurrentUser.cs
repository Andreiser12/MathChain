using MathChain.Domain.Enums;

namespace MathChain.Domain.Interfaces
{
    public interface ICurrentUser
    {
        bool IsAuthenticated { get; }
        string? WalletAddress { get; }
        UserRole? Role { get; }
    }
}
