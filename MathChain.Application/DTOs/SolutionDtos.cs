using MathChain.Domain.Enums;

namespace MathChain.Application.DTOs
{
    public class SolutionOfferDto
    {
        public Guid FormulaId { get; set; }
        public bool FreeAccess { get; set; }
        public bool CanPurchase { get; set; }
        public string TokenSymbol { get; set; } = string.Empty;
        public List<SolutionOfferItemDto> Exercises { get; set; } = new();
    }

    public class SolutionOfferItemDto
    {
        public int Index { get; set; }
        public string Difficulty { get; set; } = string.Empty;
        public bool HasSolution { get; set; }
        public bool Purchased { get; set; }
        public decimal? Price { get; set; }
    }

    public class CreateSolutionQuoteRequest
    {
        public string PayerAddress { get; set; } = string.Empty;
    }

    public class SolutionQuoteDto
    {
        public Guid PurchaseId { get; set; }
        public int Index { get; set; }
        public string Difficulty { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string TokenSymbol { get; set; } = string.Empty;
        public string TokenAddress { get; set; } = string.Empty;
        public decimal PayerBalance { get; set; }
        public string PayerAddress { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public string NetworkName { get; set; } = string.Empty;
        public long ChainId { get; set; }
        public string RpcUrl { get; set; } = string.Empty;
        public string ExplorerUrl { get; set; } = string.Empty;
        public string TypedData { get; set; } = string.Empty;
    }

    public class SubmitSolutionPaymentRequest
    {
        public string Signature { get; set; } = string.Empty;
    }

    public class SolutionPurchaseDto
    {
        public Guid PurchaseId { get; set; }
        public Guid FormulaId { get; set; }
        public int Index { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? TransactionHash { get; set; }
        public string? TransactionUrl { get; set; }
        public string? Message { get; set; }
    }

    public class ExerciseSolutionDto
    {
        public Guid ExerciseId { get; set; }
        public List<SolutionStepDto> Steps { get; set; } = new();
        public string Answer { get; set; } = string.Empty;
        public AnswerForm Form { get; set; }
    }

    public class SolutionStepDto
    {
        public string Text { get; set; } = string.Empty;
        public string? Latex { get; set; }
    }

    public class SolutionPaymentSetupDto
    {
        public bool CanRead { get; set; }
        public bool CanRelay { get; set; }
        public string NetworkName { get; set; } = string.Empty;
        public long ChainId { get; set; }
        public string RpcUrl { get; set; } = string.Empty;
        public string ExplorerUrl { get; set; } = string.Empty;
        public string ContractAddress { get; set; } = string.Empty;
        public string TokenAddress { get; set; } = string.Empty;
        public string TokenSymbol { get; set; } = string.Empty;
        public decimal? EasyPrice { get; set; }
        public decimal? MediumPrice { get; set; }
        public decimal? HardPrice { get; set; }
        public string? RelayerAddress { get; set; }
        public decimal? RelayerBalance { get; set; }
    }

    public class ChangeSolutionPriceRequest
    {
        public string Difficulty { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }

    public class SolutionPriceChangeDto
    {
        public string To { get; set; } = string.Empty;
        public string Data { get; set; } = string.Empty;
        public long PriceUnits { get; set; }
    }
}
