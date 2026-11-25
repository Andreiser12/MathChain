using MathChain.Domain.Enums;

namespace MathChain.Domain.Entities
{
    public class SmartAccountClaimRequest
    {
        public Guid UserId { get; set; }
        public string OwnerAddress { get; set; } = string.Empty;
        public CertificateKind Kind { get; set; }
        public Guid SubjectId { get; set; }
        public string DataHash { get; set; } = string.Empty;
    }

    public class SmartAccountClaimResult
    {
        public string AccountAddress { get; set; } = string.Empty;
        public string TransactionHash { get; set; } = string.Empty;
        public long TokenId { get; set; }
    }

    public class SmartAccountSetup
    {
        public bool Configured { get; set; }
        public string EntryPointAddress { get; set; } = string.Empty;
        public string FactoryAddress { get; set; } = string.Empty;
        public string PaymasterAddress { get; set; } = string.Empty;
        public string ClaimsAddress { get; set; } = string.Empty;
        public decimal PaymasterDeposit { get; set; }
        public bool ClaimsCanIssue { get; set; }
        public string GrantClaimsIssuerRoleCallData { get; set; } = string.Empty;
        public string PaymasterDepositCallData { get; set; } = string.Empty;
    }
}
