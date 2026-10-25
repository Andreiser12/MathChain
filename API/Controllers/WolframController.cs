using MathChain.Infrastructure.ResponseHandling;
using MathChain.Infrastructure.Wolfram;
using Microsoft.AspNetCore.Mvc;

namespace MathChain.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WolframController : ControllerBase
    {
        private readonly IWolframService _wolframService;
        public WolframController(IWolframService wolframService)
        {
            _wolframService = wolframService;
        }

        [HttpGet("solve")]
        public async Task<IActionResult> Solve([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest(EnvelopeExtensions.Failure<object>("Query parameter is required.", 400));
            }
            try
            {
                double result = await _wolframService.GetExactSolutionAsync(query);

                return Ok(EnvelopeExtensions.Success(result, "Wolfram Alpha evaluation completed."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, EnvelopeExtensions.Failure<object>($"Wolfram computation error: {ex.Message}", 500));
            }
        }
    }
}
