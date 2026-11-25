using MathChain.Domain.Enums;

namespace MathChain.Domain.Entities
{
    public class Certificate
    {
        public Guid Id { get; set; }
        public CertificateKind Kind { get; set; }
        public Guid SubjectId { get; set; }
        public string SubjectTitle { get; set; } = string.Empty;
        public string StudentWallet { get; set; } = string.Empty;
        public string? HolderAddress { get; set; }
        public string TeacherWallet { get; set; } = string.Empty;
        public int Grade { get; set; }
        public string Salt { get; set; } = string.Empty;
        public string DataHash { get; set; } = string.Empty;
        public long? TokenId { get; set; }
        public string? TransactionHash { get; set; }
        public CertificateStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? IssuedAt { get; set; }
        public DateTime? RevokedAt { get; set; }
    }
}
