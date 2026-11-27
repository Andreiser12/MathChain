using MathChain.Domain.Entities;
using MathChain.Domain.Exceptions;
using MathChain.Domain.Interfaces;

namespace MathChain.Tests.Shared.Fakes
{
    public class FakeSmartAccountService : ISmartAccountService
    {
        private readonly FakeCertificateRegistry _registry;

        public FakeSmartAccountService(FakeCertificateRegistry registry)
        {
            _registry = registry;
        }

        public bool IsConfigured { get; set; } = true;
        public bool FailBeforeMining { get; set; }
        public bool FailAfterMining { get; set; }
        public List<SmartAccountClaimRequest> Claims { get; } = new();
        public List<long> RevokedTokenIds { get; } = new();
        public HashSet<string> DeployedAccounts { get; } = new();

        public static string AccountOf(string ownerAddress)
        {
            return "0xacc0" + ownerAddress.Trim().ToLowerInvariant()[6..];
        }

        public Task<string> GetAccountAddressAsync(string ownerAddress)
        {
            return Task.FromResult(AccountOf(ownerAddress));
        }

        public Task<bool> IsDeployedAsync(string accountAddress)
        {
            return Task.FromResult(DeployedAccounts.Contains(accountAddress));
        }

        public Task<SmartAccountClaimResult> ClaimCertificateAsync(SmartAccountClaimRequest request)
        {
            Claims.Add(request);

            if (FailBeforeMining)
            {
                throw new ServiceUnavailableException("The blockchain network could not be reached. Try again later.");
            }

            var account = AccountOf(request.OwnerAddress);

            if (_registry.Tokens.Any(t => !t.Revoked && t.StudentAddress == account && t.Kind == request.Kind && t.SubjectId == request.SubjectId))
            {
                throw new BusinessRuleException("This smart account already holds an active certificate for this subject.");
            }

            var token = new OnChainCertificate
            {
                TokenId = _registry.Tokens.Count + 1,
                StudentAddress = account,
                IssuerAddress = FakeCertificateRegistry.IssuerAddress,
                Kind = request.Kind,
                SubjectId = request.SubjectId,
                DataHash = request.DataHash,
                IssuedAt = new DateTime(2026, 11, 25, 12, 0, 0, DateTimeKind.Utc)
            };
            _registry.Tokens.Add(token);
            DeployedAccounts.Add(account);

            if (FailAfterMining)
            {
                throw new ServiceUnavailableException("The blockchain network could not be reached. Try again later.");
            }

            return Task.FromResult(new SmartAccountClaimResult
            {
                AccountAddress = account,
                TokenId = token.TokenId,
                TransactionHash = "0x" + Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N")
            });
        }

        public Task<string> RevokeClaimedCertificateAsync(long tokenId)
        {
            var token = _registry.Tokens.First(t => t.TokenId == tokenId);
            token.Revoked = true;
            RevokedTokenIds.Add(tokenId);

            return Task.FromResult("0x" + Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"));
        }

        public Task<SmartAccountSetup> GetSetupAsync()
        {
            return Task.FromResult(new SmartAccountSetup
            {
                Configured = IsConfigured,
                EntryPointAddress = "0x0000000071727de22e5e9d8baf0edac6f37da032",
                FactoryAddress = "0xfac0000000000000000000000000000000000001",
                PaymasterAddress = "0xba70000000000000000000000000000000000002",
                ClaimsAddress = "0xc1a1000000000000000000000000000000000003",
                PaymasterDeposit = 0.02m,
                ClaimsCanIssue = true,
                GrantClaimsIssuerRoleCallData = "0x2f2ff15d",
                PaymasterDepositCallData = "0xd0e30db0"
            });
        }
    }
}
