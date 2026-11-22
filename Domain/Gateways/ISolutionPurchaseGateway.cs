using MathChain.Domain.Entities;

namespace MathChain.Domain.Gateways
{
    public interface ISolutionPurchaseGateway
    {
        Task<SolutionPurchase?> GetByIdAsync(Guid id);
        Task AddAsync(SolutionPurchase purchase);
        Task<List<int>> GetPurchasedIndicesAsync(string accountWallet, Guid formulaId);
        Task<bool> HasInProgressAsync(string accountWallet, Guid formulaId, int exerciseIndex);
        Task<bool> TryStartSubmittingAsync(Guid id);
        Task ReturnToQuotedAsync(Guid id);
        Task MarkSubmittedAsync(Guid id, string transactionHash);
        Task<bool> TryCompleteAsync(Guid id, DateTime completedAt);
        Task MarkFailedAsync(Guid id, string reason);
    }
}
