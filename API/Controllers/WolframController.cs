using MathChain.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace MathChain.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WolframController: ControllerBase
    {
        private readonly WolframService _wolframService;

        public WolframController(WolframService wolframService)
        {
            _wolframService = wolframService;
        }

        [HttpGet("solve")]
        public async Task<IActionResult> Solve([FromQuery] string query)
        {
            if(string.IsNullOrEmpty(query))
                return BadRequest("Query is required.");

            double result = await _wolframService.GetExactSolutionAsync(query);
            return Ok(result);
        }
    }
}
