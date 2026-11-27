using MathChain.Domain.Entities;

namespace MathChain.Domain.Gateways
{
    public sealed record IndexedChainEvent(ChainLogEvent Event, DateTime BlockTime, string? Cid = null);

    public interface IChainIndexGateway
    {
        Task<IndexerCheckpoint?> GetCheckpointAsync(string id);
        Task ApplyAsync(string checkpointId, long chainId, IReadOnlyList<IndexedChainEvent> events, long lastBlock, DateTime now);
        Task<ChainIndexStats> GetStatsAsync();
        Task<ChainCertificate?> FindCertificateAsync(long tokenId);
        IQueryable<ChainCertificate> Certificates { get; }
        IQueryable<ChainSmartAccount> SmartAccounts { get; }
        IQueryable<ChainSolutionPurchase> SolutionPurchases { get; }
        IQueryable<ChainPriceChange> PriceChanges { get; }
        IQueryable<ChainCurriculumPublication> CurriculumPublications { get; }
    }
}
