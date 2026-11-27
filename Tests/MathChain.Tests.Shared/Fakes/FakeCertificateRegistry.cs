using MathChain.Blockchain.Certificates;
using MathChain.Domain.Entities;
using MathChain.Domain.Enums;
using MathChain.Domain.Exceptions;
using MathChain.Domain.Interfaces;

namespace MathChain.Tests.Shared.Fakes
{
    public class FakeCertificateRegistry : ICertificateRegistry
    {
        public const string IssuerAddress = "0x70997970c51812dc3a010c7d01b50e0d17dc79c8";
        public const string ContractAddress = "0x1b09a2e38ef9bfc02bb4837c01cff1533652a882";

        public List<OnChainCertificate> Tokens { get; } = new();
        public List<long> RevokedTokenIds { get; } = new();
        public int IssueCalls { get; private set; }
        public bool CanRead { get; set; } = true;
        public bool CanIssue { get; set; } = true;
        public bool HasIssuerRole { get; set; } = true;
        public bool FailBeforeMining { get; set; }
        public bool FailAfterMining { get; set; }
        public Dictionary<string, decimal> Balances { get; } = new();

        public CertificateNetworkInfo Network => new()
        {
            NetworkName = "Test Network",
            ChainId = 31337,
            RpcUrl = "http://127.0.0.1:8545",
            ExplorerUrl = "https://explorer.example",
            ContractAddress = ContractAddress,
            IssuerAddress = CanIssue ? IssuerAddress : null
        };

        public string CreateSalt()
        {
            return CertificateEncoding.CreateSalt();
        }

        public string ComputeDataHash(CertificateCommitment commitment)
        {
            return CertificateEncoding.ComputeDataHash(commitment);
        }

        public string BuildGrantIssuerRoleCallData()
        {
            return CertificateEncoding.BuildGrantIssuerRoleCallData(IssuerAddress);
        }

        public Task<CertificateBatchResult> IssueBatchAsync(CertificateKind kind, Guid subjectId, IReadOnlyList<CertificateIssueItem> items)
        {
            IssueCalls++;

            if (FailBeforeMining)
            {
                throw new ServiceUnavailableException("The blockchain network could not be reached. Try again later.");
            }

            if (items.Any(item => FindActive(item.StudentAddress, kind, subjectId) != 0)
                || items.Select(item => item.StudentAddress.ToLowerInvariant()).Distinct().Count() != items.Count)
            {
                throw new BusinessRuleException("The blockchain already holds an active certificate for one of these students.");
            }

            var result = new CertificateBatchResult { TransactionHash = "0x" + Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N") };

            foreach (var item in items)
            {
                var token = new OnChainCertificate
                {
                    TokenId = Tokens.Count + 1,
                    StudentAddress = item.StudentAddress.ToLowerInvariant(),
                    IssuerAddress = IssuerAddress,
                    Kind = kind,
                    SubjectId = subjectId,
                    DataHash = item.DataHash,
                    IssuedAt = new DateTime(2026, 11, 14, 12, 0, 0, DateTimeKind.Utc)
                };
                Tokens.Add(token);
                result.Tokens.Add(new IssuedCertificateToken { StudentAddress = token.StudentAddress, TokenId = token.TokenId });
            }

            if (FailAfterMining)
            {
                throw new ServiceUnavailableException("The blockchain network could not be reached. Try again later.");
            }

            return Task.FromResult(result);
        }

        public Task<string> RevokeAsync(long tokenId)
        {
            var token = Tokens.FirstOrDefault(t => t.TokenId == tokenId);

            if (token == null || token.Revoked)
            {
                return Task.FromResult(string.Empty);
            }

            token.Revoked = true;
            RevokedTokenIds.Add(tokenId);

            return Task.FromResult("0x" + Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"));
        }

        public Task<OnChainCertificate?> GetAsync(long tokenId)
        {
            return Task.FromResult(EntityCopy.Detached(Tokens.FirstOrDefault(t => t.TokenId == tokenId)));
        }

        public Task<long> FindActiveAsync(string studentAddress, CertificateKind kind, Guid subjectId)
        {
            return Task.FromResult(FindActive(studentAddress, kind, subjectId));
        }

        public Task<CertificateIssuerStatus> GetIssuerStatusAsync()
        {
            return Task.FromResult(new CertificateIssuerStatus { HasIssuerRole = HasIssuerRole, Balance = 0.005m });
        }

        public Task<decimal> GetBalanceAsync(string address)
        {
            return Task.FromResult(Balances.GetValueOrDefault(address, 0m));
        }

        private long FindActive(string studentAddress, CertificateKind kind, Guid subjectId)
        {
            var student = studentAddress.Trim().ToLowerInvariant();

            return Tokens.FirstOrDefault(t => !t.Revoked && t.StudentAddress == student && t.Kind == kind && t.SubjectId == subjectId)?.TokenId ?? 0;
        }
    }
}
