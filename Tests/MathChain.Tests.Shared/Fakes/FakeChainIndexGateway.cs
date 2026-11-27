using MathChain.Domain.Entities;
using MathChain.Domain.Gateways;

namespace MathChain.Tests.Shared.Fakes
{
    public class FakeChainIndexGateway : IChainIndexGateway
    {
        public List<ChainCertificate> CertificateRows { get; } = new();
        public List<ChainSmartAccount> SmartAccountRows { get; } = new();
        public List<ChainSolutionPurchase> PurchaseRows { get; } = new();
        public List<ChainPriceChange> PriceRows { get; } = new();
        public List<ChainCurriculumPublication> PublicationRows { get; } = new();
        public IndexerCheckpoint? Checkpoint { get; set; }

        public IQueryable<ChainCertificate> Certificates => CertificateRows.AsQueryable();
        public IQueryable<ChainSmartAccount> SmartAccounts => SmartAccountRows.AsQueryable();
        public IQueryable<ChainSolutionPurchase> SolutionPurchases => PurchaseRows.AsQueryable();
        public IQueryable<ChainPriceChange> PriceChanges => PriceRows.AsQueryable();
        public IQueryable<ChainCurriculumPublication> CurriculumPublications => PublicationRows.AsQueryable();

        public Task<IndexerCheckpoint?> GetCheckpointAsync(string id)
        {
            return Task.FromResult(Checkpoint);
        }

        public Task ApplyAsync(string checkpointId, long chainId, IReadOnlyList<IndexedChainEvent> events, long lastBlock, DateTime now)
        {
            Checkpoint = new IndexerCheckpoint { Id = checkpointId, ChainId = chainId, LastBlock = lastBlock, UpdatedAt = now };
            return Task.CompletedTask;
        }

        public Task<ChainCertificate?> FindCertificateAsync(long tokenId)
        {
            return Task.FromResult(CertificateRows.FirstOrDefault(c => c.TokenId == tokenId));
        }

        public Task<ChainIndexStats> GetStatsAsync()
        {
            return Task.FromResult(new ChainIndexStats
            {
                Certificates = CertificateRows.Count,
                ActiveCertificates = CertificateRows.Count(c => !c.Revoked),
                RevokedCertificates = CertificateRows.Count(c => c.Revoked),
                CertificatesClaimedBySmartAccounts = CertificateRows.Count(c => c.ClaimedBySmartAccount),
                SmartAccounts = SmartAccountRows.Count,
                SolutionPurchases = PurchaseRows.Count,
                SolutionRevenue = PurchaseRows.Sum(p => p.Price),
                CurriculumVersions = PublicationRows.Count
            });
        }
    }
}
