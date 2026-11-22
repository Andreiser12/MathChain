using MathChain.Application.Contexts;
using MathChain.Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace MathChain.API.Controllers
{
    [ApiController]
    [Route("api/solutions")]
    public class SolutionsController : ApiControllerBase
    {
        private readonly ISolutionContext _solutionContext;

        public SolutionsController(ISolutionContext solutionContext)
        {
            _solutionContext = solutionContext;
        }

        [HttpGet("formulas/{formulaId:guid}")]
        public async Task<IActionResult> GetOffer(Guid formulaId)
        {
            var offer = await _solutionContext.GetOfferAsync(formulaId);

            return Success(offer);
        }

        [HttpPost("formulas/{formulaId:guid}/exercises/{index:int}/quote")]
        public async Task<IActionResult> CreateQuote(Guid formulaId, int index, [FromBody] CreateSolutionQuoteRequest request)
        {
            var quote = await _solutionContext.CreateQuoteAsync(formulaId, index, request);

            return Success(quote);
        }

        [HttpPost("purchases/{purchaseId:guid}")]
        public async Task<IActionResult> SubmitPayment(Guid purchaseId, [FromBody] SubmitSolutionPaymentRequest request)
        {
            var purchase = await _solutionContext.SubmitPaymentAsync(purchaseId, request);

            return Success(purchase, purchase.Message ?? string.Empty);
        }

        [HttpGet("purchases/{purchaseId:guid}")]
        public async Task<IActionResult> GetPurchase(Guid purchaseId)
        {
            var purchase = await _solutionContext.GetPurchaseAsync(purchaseId);

            return Success(purchase, purchase.Message ?? string.Empty);
        }

        [HttpGet("exercises/{exerciseId:guid}")]
        public async Task<IActionResult> GetSolution(Guid exerciseId)
        {
            var solution = await _solutionContext.GetSolutionAsync(exerciseId);

            return Success(solution);
        }

        [HttpGet("setup")]
        public async Task<IActionResult> GetSetup()
        {
            var setup = await _solutionContext.GetPaymentSetupAsync();

            return Success(setup);
        }

        [HttpPost("setup/prices")]
        public IActionResult BuildPriceChange([FromBody] ChangeSolutionPriceRequest request)
        {
            var change = _solutionContext.BuildPriceChange(request);

            return Success(change);
        }
    }
}
