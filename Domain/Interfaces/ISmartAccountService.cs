using MathChain.Domain.Entities;

namespace MathChain.Domain.Interfaces
{
    public interface ISmartAccountService
    {
        bool IsConfigured { get; }

        Task<string> GetAccountAddressAsync(string ownerAddress);
        Task<bool> IsDeployedAsync(string accountAddress);
        Task<SmartAccountClaimResult> ClaimCertificateAsync(SmartAccountClaimRequest request);
        Task<string> RevokeClaimedCertificateAsync(long tokenId);
        Task<SmartAccountSetup> GetSetupAsync();
    }
}
