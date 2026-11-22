using MathChain.Domain.Enums;

namespace MathChain.Domain.Entities
{
    public class SolutionPurchase
    {
        public Guid Id { get; set; }
        public string AccountWallet { get; set; } = string.Empty;
        public Guid FormulaId { get; set; }
        public int ExerciseIndex { get; set; }
        public SolutionTier Tier { get; set; }
        public long PriceUnits { get; set; }
        public string PayerAddress { get; set; } = string.Empty;
        public string SolutionId { get; set; } = string.Empty;
        public string Salt { get; set; } = string.Empty;
        public string Nonce { get; set; } = string.Empty;
        public DateTime ValidBefore { get; set; }
        public SolutionPurchaseStatus Status { get; set; }
        public string? TransactionHash { get; set; }
        public string? FailureReason { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }
}
