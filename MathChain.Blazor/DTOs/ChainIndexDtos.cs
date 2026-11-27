using MathChain.Domain.Enums;

namespace MathChain.Blazor.DTOs
{
    public class ChainIndexStatsDto
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

    public class ChainIndexerStatusDto
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

    public class ChainIndexOverviewDto
    {
        public ChainIndexStatsDto Stats { get; set; } = new();
        public ChainIndexerStatusDto IndexerStatus { get; set; } = new();
    }

    public class ChainCertificateDto
    {
        public long TokenId { get; set; }
        public string Holder { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public CertificateKind Kind { get; set; }
        public bool ClaimedBySmartAccount { get; set; }
        public long IssuedBlock { get; set; }
        public DateTime IssuedAt { get; set; }
        public string IssuedTransaction { get; set; } = string.Empty;
        public bool Revoked { get; set; }
        public string? RevokedBy { get; set; }
        public DateTime? RevokedAt { get; set; }
        public string? RevokedTransaction { get; set; }
    }

    public class ChainCertificateResultDto
    {
        public ChainCertificateDto? Certificate { get; set; }
        public ChainIndexerStatusDto IndexerStatus { get; set; } = new();
    }
}
