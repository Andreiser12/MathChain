using MathChain.Domain.Entities;

namespace MathChain.Domain.Interfaces
{
    public interface IGoogleTokenVerifier
    {
        string? ClientId { get; }
        Task<GoogleIdentity> VerifyAsync(string idToken);
    }
}
