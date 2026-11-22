using MathChain.Domain.Enums;

namespace MathChain.Domain.Entities
{
    public class SolutionPrices
    {
        public long Easy { get; set; }
        public long Medium { get; set; }
        public long Hard { get; set; }

        public long For(SolutionTier tier)
        {
            return tier switch
            {
                SolutionTier.Easy => Easy,
                SolutionTier.Medium => Medium,
                SolutionTier.Hard => Hard,
                _ => throw new ArgumentOutOfRangeException(nameof(tier))
            };
        }
    }

    public class SolutionPaymentNetwork
    {
        public string NetworkName { get; set; } = string.Empty;
        public long ChainId { get; set; }
        public string RpcUrl { get; set; } = string.Empty;
        public string ExplorerUrl { get; set; } = string.Empty;
        public string ContractAddress { get; set; } = string.Empty;
        public string TokenAddress { get; set; } = string.Empty;
        public string TokenName { get; set; } = string.Empty;
        public string TokenVersion { get; set; } = string.Empty;
        public string TokenSymbol { get; set; } = string.Empty;
        public int TokenDecimals { get; set; }
        public string? RelayerAddress { get; set; }
    }

    public class SolutionPaymentOrder
    {
        public string AccountAddress { get; set; } = string.Empty;
        public string SolutionId { get; set; } = string.Empty;
        public SolutionTier Tier { get; set; }
        public string Salt { get; set; } = string.Empty;
        public string PayerAddress { get; set; } = string.Empty;
        public long PriceUnits { get; set; }
        public long ValidBefore { get; set; }
        public string Signature { get; set; } = string.Empty;
    }

    public enum SolutionPaymentState
    {
        Pending,
        Confirmed,
        Reverted
    }

    public class SolutionPaymentOutcome
    {
        public SolutionPaymentState State { get; set; }
        public string? AccountAddress { get; set; }
        public string? SolutionId { get; set; }
        public string? PayerAddress { get; set; }
        public SolutionTier? Tier { get; set; }
        public long? PriceUnits { get; set; }
    }

    public class SolutionRelayerStatus
    {
        public string Address { get; set; } = string.Empty;
        public decimal Balance { get; set; }
    }

    public class ExerciseSolution
    {
        public List<SolutionStep> Steps { get; set; } = new();
    }

    public class SolutionStep
    {
        public string Text { get; set; } = string.Empty;
        public string? Latex { get; set; }
    }
}
