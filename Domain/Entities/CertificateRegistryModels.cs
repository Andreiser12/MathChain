using MathChain.Domain.Enums;

namespace MathChain.Domain.Entities
{
    public class CertificateCommitment
    {
        public string StudentAddress { get; set; } = string.Empty;
        public CertificateKind Kind { get; set; }
        public Guid SubjectId { get; set; }
        public int Grade { get; set; }
        public string TeacherAddress { get; set; } = string.Empty;
        public string Salt { get; set; } = string.Empty;
    }

    public class CertificateIssueItem
    {
        public string StudentAddress { get; set; } = string.Empty;
        public string DataHash { get; set; } = string.Empty;
    }

    public class IssuedCertificateToken
    {
        public string StudentAddress { get; set; } = string.Empty;
        public long TokenId { get; set; }
    }

    public class CertificateBatchResult
    {
        public string TransactionHash { get; set; } = string.Empty;
        public List<IssuedCertificateToken> Tokens { get; set; } = new();
    }

    public class OnChainCertificate
    {
        public long TokenId { get; set; }
        public string StudentAddress { get; set; } = string.Empty;
        public string IssuerAddress { get; set; } = string.Empty;
        public CertificateKind Kind { get; set; }
        public Guid SubjectId { get; set; }
        public string DataHash { get; set; } = string.Empty;
        public DateTime IssuedAt { get; set; }
        public bool Revoked { get; set; }
    }

    public class CertificateNetworkInfo
    {
        public string NetworkName { get; set; } = string.Empty;
        public long ChainId { get; set; }
        public string RpcUrl { get; set; } = string.Empty;
        public string ExplorerUrl { get; set; } = string.Empty;
        public string ContractAddress { get; set; } = string.Empty;
        public string? IssuerAddress { get; set; }
    }

    public class CertificateIssuerStatus
    {
        public bool HasIssuerRole { get; set; }
        public decimal Balance { get; set; }
    }
}
