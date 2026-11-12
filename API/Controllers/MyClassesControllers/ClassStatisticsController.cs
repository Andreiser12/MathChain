using MathChain.Application.Contexts;
using Microsoft.AspNetCore.Mvc;

namespace MathChain.API.Controllers.MyClassesControllers
{
    [ApiController]
    [Route("api/classroom")]
    public class ClassStatisticsController : ControllerBase
    {
        private readonly IClassStatisticsContext _statisticsContext;

        public ClassStatisticsController(IClassStatisticsContext statisticsContext)
        {
            _statisticsContext = statisticsContext;
        }

        [HttpGet("{classRoomId}/statistics")]
        public async Task<IActionResult> GetClassStatistics(Guid classRoomId)
        {
            var statistics = await _statisticsContext.GetClassStatisticsAsync(classRoomId);

            return Ok(statistics);
        }
    }
}
