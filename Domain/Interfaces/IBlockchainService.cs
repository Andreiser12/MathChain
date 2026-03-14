using System;
using System.Threading.Tasks;

namespace MathChain.Domain.Interfaces
{
    public interface IBlockchainService
    {
        Task<string> SubmitSolutionAsync(Guid problemId, string walletAddress, double solution);
        Task<bool> VerifySolutionAsync(Guid problemId, double solution);
        Task<string> PayForSolutionAsync(Guid problemId, string walletAddress);
        Task<decimal> GetWalletBalanceAsync(string walletAddress);
    }
}
