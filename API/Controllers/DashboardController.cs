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
        public async Task<IActionResult> GetDashboardStats()
        {
            var stats = await _dashboardContext.GetDashboardStatsAsync();

            return Success(stats);
        }
    }
}
