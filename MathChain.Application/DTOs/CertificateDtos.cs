using MathChain.Domain.Enums;

namespace MathChain.Application.DTOs
{
    public class IssueCertificatesRequest
    {
        public List<string>? StudentWallets { get; set; }
    }

    public class ClassCertificateDto
    {
        public Guid Id { get; set; }
        public string StudentWallet { get; set; } = string.Empty;
        public int Grade { get; set; }
        public CertificateStatus Status { get; set; }
        public long? TokenId { get; set; }
        public string? TransactionHash { get; set; }
        public DateTime? IssuedAt { get; set; }
    }

    public class MyCertificateDto
    {
        public Guid Id { get; set; }
        public CertificateKind Kind { get; set; }
        public Guid SubjectId { get; set; }
        public string SubjectTitle { get; set; } = string.Empty;
        public int Grade { get; set; }
        public CertificateStatus Status { get; set; }
        public long? TokenId { get; set; }
        public string VerificationKey { get; set; } = string.Empty;
        public string? TransactionHash { get; set; }
        public DateTime? IssuedAt { get; set; }
        public string? HolderAddress { get; set; }
    }

    public class ChapterCertificateProgressDto
    {
        public Guid ChapterId { get; set; }
        public string Title { get; set; } = string.Empty;
        public bool CertificatesAvailable { get; set; }
        public bool Completed { get; set; }
        public int TotalExercises { get; set; }
        public int SolvedExercises { get; set; }
        public List<ChapterFormulaProgressDto> Formulas { get; set; } = new();
        public MyCertificateDto? Certificate { get; set; }
    }

    public class ChapterFormulaProgressDto
    {
        public Guid FormulaId { get; set; }
        public string Title { get; set; } = string.Empty;
        public bool UsesGenericExercises { get; set; }
        public int TotalExercises { get; set; }
        public int SolvedExercises { get; set; }
    }

    public class CertificateVerificationDto
    {
        public long TokenId { get; set; }
        public CertificateKind Kind { get; set; }
        public string StudentAddress { get; set; } = string.Empty;
        public string IssuerAddress { get; set; } = string.Empty;
        public DateTime IssuedAt { get; set; }
        public bool Revoked { get; set; }
        public bool IsValid { get; set; }
        public bool DetailsRevealed { get; set; }
        public bool? DataMatchesBlockchain { get; set; }
        public string? SubjectTitle { get; set; }
        public int? Grade { get; set; }
        public string? TeacherAddress { get; set; }
        public string? TransactionHash { get; set; }
        public string NetworkName { get; set; } = string.Empty;
        public long ChainId { get; set; }
        public string ContractAddress { get; set; } = string.Empty;
        public string ExplorerUrl { get; set; } = string.Empty;
    }

    public class CertificateSetupDto
    {
        public string NetworkName { get; set; } = string.Empty;
        public long ChainId { get; set; }
        public string RpcUrl { get; set; } = string.Empty;
        public string ExplorerUrl { get; set; } = string.Empty;
        public string ContractAddress { get; set; } = string.Empty;
        public bool CanRead { get; set; }
        public bool CanIssue { get; set; }
        public string? IssuerAddress { get; set; }
        public bool HasIssuerRole { get; set; }
        public decimal IssuerBalance { get; set; }
        public string? GrantIssuerRoleCallData { get; set; }
        public bool SmartAccountsConfigured { get; set; }
        public string? EntryPointAddress { get; set; }
        public string? AccountFactoryAddress { get; set; }
        public string? PaymasterAddress { get; set; }
        public string? ClaimsAddress { get; set; }
        public decimal PaymasterDeposit { get; set; }
        public bool ClaimsCanIssue { get; set; }
        public string? GrantClaimsIssuerRoleCallData { get; set; }
        public string? PaymasterDepositCallData { get; set; }
    }

    public class SmartAccountDto
    {
        public bool Available { get; set; }
        public string? Address { get; set; }
        public bool Deployed { get; set; }
        public string? ExplorerUrl { get; set; }
    }
}
