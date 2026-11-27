using MathChain.Domain.Enums;

namespace MathChain.Domain.Entities
{
    public class ChainCertificate
    {
        public long TokenId { get; set; }
        public string Holder { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public CertificateKind Kind { get; set; }
        public string SubjectId { get; set; } = string.Empty;
        public string DataHash { get; set; } = string.Empty;
        public bool ClaimedBySmartAccount { get; set; }
        public long IssuedBlock { get; set; }
        public DateTime IssuedAt { get; set; }
        public string IssuedTransaction { get; set; } = string.Empty;
        public bool Revoked { get; set; }
        public string? RevokedBy { get; set; }
        public DateTime? RevokedAt { get; set; }
        public string? RevokedTransaction { get; set; }
    }

    public class ChainSmartAccount
    {
        public string Account { get; set; } = string.Empty;
        public string Owner { get; set; } = string.Empty;
        public long CreatedBlock { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Transaction { get; set; } = string.Empty;
    }

    public class ChainSolutionPurchase
    {
        public string Id { get; set; } = string.Empty;
        public string Account { get; set; } = string.Empty;
        public string SolutionId { get; set; } = string.Empty;
        public string Payer { get; set; } = string.Empty;
        public int Tier { get; set; }
        public decimal Price { get; set; }
        public long Block { get; set; }
        public DateTime PurchasedAt { get; set; }
        public string Transaction { get; set; } = string.Empty;
    }

    public class ChainPriceChange
    {
        public string Id { get; set; } = string.Empty;
        public int Tier { get; set; }
        public decimal Price { get; set; }
        public long Block { get; set; }
        public DateTime ChangedAt { get; set; }
        public string Transaction { get; set; } = string.Empty;
    }

    public class ChainCurriculumPublication
    {
        public long Version { get; set; }
        public string Cid { get; set; } = string.Empty;
        public string Publisher { get; set; } = string.Empty;
        public DateTime PublishedAt { get; set; }
        public long Block { get; set; }
        public string Transaction { get; set; } = string.Empty;
    }

    public class IndexerCheckpoint
    {
        public string Id { get; set; } = string.Empty;
        public long ChainId { get; set; }
        public long LastBlock { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public abstract record ChainLogEvent
    {
        public long BlockNumber { get; init; }
        public string TransactionHash { get; init; } = string.Empty;
        public int LogIndex { get; init; }
        public string Contract { get; init; } = string.Empty;
        public string Id => $"{TransactionHash}-{LogIndex}";
    }

    public sealed record CertificateIssuedLog(long TokenId, string Student, string Issuer, CertificateKind Kind, string SubjectId, string DataHash) : ChainLogEvent;

    public sealed record CertificateRevokedLog(long TokenId, string RevokedBy) : ChainLogEvent;

    public sealed record CertificateClaimedLog(long TokenId, string Student) : ChainLogEvent;

    public sealed record AccountCreatedLog(string Account, string Owner) : ChainLogEvent;

    public sealed record SolutionPurchasedLog(string Account, string SolutionId, string Payer, int Tier, decimal Price) : ChainLogEvent;

    public sealed record PriceChangedLog(int Tier, decimal Price) : ChainLogEvent;

    public sealed record CurriculumPublishedLog(long Version, byte[] Cid, string Publisher) : ChainLogEvent;

    public sealed class ChainIndexStats
    {
        public int Certificates { get; set; }
        public int ActiveCertificates { get; set; }
        public int RevokedCertificates { get; set; }
        public int CertificatesClaimedBySmartAccounts { get; set; }
        public int SmartAccounts { get; set; }
        public int SolutionPurchases { get; set; }
        public decimal SolutionRevenue { get; set; }
        public int CurriculumVersions { get; set; }
    }

    public sealed class ChainIndexerStatus
    {
        public bool Enabled { get; set; }
        public long ChainId { get; set; }
        public long StartBlock { get; set; }
        public long? LastIndexedBlock { get; set; }
        public long? ChainHead { get; set; }
        public int Confirmations { get; set; }
        public long? ConfirmedHead { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
