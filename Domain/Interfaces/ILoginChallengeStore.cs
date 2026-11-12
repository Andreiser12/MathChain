using MathChain.Domain.Entities;

namespace MathChain.Domain.Interfaces
{
    public interface ILoginChallengeStore
    {
        void Save(LoginChallenge challenge);
        LoginChallenge? Take(string walletAddress);
    }
}
