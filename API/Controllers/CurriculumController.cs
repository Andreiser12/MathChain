using MathChain.Application.Contexts;
using Microsoft.AspNetCore.Mvc;

namespace MathChain.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CurriculumController : ControllerBase
    {
        private readonly ICurriculumContext _curriculumContext;
        public CurriculumController(ICurriculumContext curriculumContext)
        {
            _curriculumContext = curriculumContext;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllChapters()
        {
            var result = await _curriculumContext.GetAllChaptersAsync();

            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("{slug}")]
        public async Task<IActionResult> GetChapterBySlug(string slug)
        {
            var result = await _curriculumContext.GetChapterBySlugAsync(slug);

            return StatusCode(result.StatusCode, result);
        }

        [HttpPut("{slug}/ipfs")]
        public async Task<IActionResult> UpdateIpfsHash(string slug, [FromBody] UpdateIpfsHashRequest request)
        {
            var result = await _curriculumContext.UpdateIpfsHashAsync(slug, request.IpfsHash);

            return StatusCode(result.StatusCode, result);
        }

        public class UpdateIpfsHashRequest
        {
            public string IpfsHash { get; set; } = string.Empty;
        }
    }
}
