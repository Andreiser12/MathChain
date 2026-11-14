using MathChain.Domain.Entities;
using MathChain.Domain.Enums;

namespace MathChain.Domain.Interfaces
{
    public interface ICertificateRegistry
    {
        bool CanRead { get; }
        bool CanIssue { get; }
        CertificateNetworkInfo Network { get; }

        string CreateSalt();
        string ComputeDataHash(CertificateCommitment commitment);
        string BuildGrantIssuerRoleCallData();

        Task<CertificateBatchResult> IssueBatchAsync(CertificateKind kind, Guid subjectId, IReadOnlyList<CertificateIssueItem> items);
        Task<string> RevokeAsync(long tokenId);
        Task<OnChainCertificate?> GetAsync(long tokenId);
        Task<long> FindActiveAsync(string studentAddress, CertificateKind kind, Guid subjectId);
        Task<CertificateIssuerStatus> GetIssuerStatusAsync();
        Task<decimal> GetBalanceAsync(string address);
    }
}
