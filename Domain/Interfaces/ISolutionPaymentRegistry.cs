using MathChain.Domain.Entities;
using MathChain.Domain.Enums;

namespace MathChain.Domain.Interfaces
{
    public interface ISolutionPaymentRegistry
    {
        bool CanRead { get; }
        bool CanRelay { get; }
        SolutionPaymentNetwork Network { get; }

        string CreateSalt();
        string ComputeSolutionId(Guid formulaId, int exerciseIndex);
        string ComputeNonce(string accountAddress, string solutionId, SolutionTier tier, string salt);
        bool IsSignedByPayer(SolutionPaymentOrder order);
        string BuildSetPriceCallData(SolutionTier tier, long priceUnits);

        Task<SolutionPrices> GetPricesAsync();
        Task<long> GetTokenBalanceAsync(string address);
        Task<bool> IsPurchasedAsync(string accountAddress, string solutionId);
        Task<string> SubmitAsync(SolutionPaymentOrder order);
        Task<SolutionPaymentOutcome> GetOutcomeAsync(string transactionHash);
        Task<SolutionRelayerStatus> GetRelayerStatusAsync();
    }
}
