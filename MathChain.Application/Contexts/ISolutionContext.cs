using MathChain.Application.DTOs;

namespace MathChain.Application.Contexts
{
    public interface ISolutionContext
    {
        Task<SolutionOfferDto> GetOfferAsync(Guid formulaId);
        Task<SolutionQuoteDto> CreateQuoteAsync(Guid formulaId, int index, CreateSolutionQuoteRequest request);
        Task<SolutionPurchaseDto> SubmitPaymentAsync(Guid purchaseId, SubmitSolutionPaymentRequest request);
        Task<SolutionPurchaseDto> GetPurchaseAsync(Guid purchaseId);
        Task<ExerciseSolutionDto> GetSolutionAsync(Guid exerciseId);
        Task<SolutionPaymentSetupDto> GetPaymentSetupAsync();
        SolutionPriceChangeDto BuildPriceChange(ChangeSolutionPriceRequest request);
    }
}
