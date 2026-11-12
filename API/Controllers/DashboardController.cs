using MathChain.Application.Contexts;
using MathChain.Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace MathChain.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ApiControllerBase
    {
        private readonly IDashboardContext _dashboardContext;

        public DashboardController(IDashboardContext dashboardContext)
        {
            _dashboardContext = dashboardContext;
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboardStats([FromQuery] string? walletAddress)
        {
            var stats = await _dashboardContext.GetDashboardStatsAsync(walletAddress);

            return Success(stats);
        }

        [HttpPost("record-solved")]
        public async Task<IActionResult> RecordSolvedProblem([FromBody] RecordSolvedProblemRequest request)
        {
            var result = await _dashboardContext.RecordSolvedProblemAsync(request);

            return Success(result);
        }

        [HttpGet("practice-progress")]
        public async Task<IActionResult> GetPracticeProgress([FromQuery] string walletAddress, [FromQuery] string formulaId)
        {
            var progress = await _dashboardContext.GetPracticeProgressAsync(walletAddress, formulaId);

            return Success(progress);
        }
    }
}
