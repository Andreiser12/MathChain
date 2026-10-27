using MathChain.Application.Contexts;
using MathChain.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

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
        public async Task<IActionResult> GetAllChapters([FromQuery] bool rootsOnly = false)
        {
            if (rootsOnly)
            {
                var rootResult = await _curriculumContext.GetRootChaptersAsync();
                return StatusCode(rootResult.StatusCode, rootResult);
            }

            var result = await _curriculumContext.GetAllChaptersAsync();
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("roots")]
        public async Task<IActionResult> GetRootChapters()
        {
            var result = await _curriculumContext.GetRootChaptersAsync();
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("{idOrSlug}")]
        public async Task<IActionResult> GetChapterDetail(string idOrSlug)
        {
            var result = await _curriculumContext.GetChapterDetailAsync(idOrSlug);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost]
        public async Task<IActionResult> CreateChapter([FromBody] CreateChapterRequest request)
        {
            var result = await _curriculumContext.CreateChapterAsync(request);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateChapter(Guid id, [FromBody] UpdateChapterRequest request)
        {
            var result = await _curriculumContext.UpdateChapterAsync(id, request);
            return StatusCode(result.StatusCode, result);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteChapter(Guid id)
        {
            var result = await _curriculumContext.DeleteChapterAsync(id);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPut("{slug}/ipfs")]
        public async Task<IActionResult> UpdateIpfsHash(string slug, [FromBody] UpdateIpfsHashRequest request)
        {
            var result = await _curriculumContext.UpdateIpfsHashAsync(slug, request.IpfsHash);
            return StatusCode(result.StatusCode, result);
        }

        // Formula endpoints
        [HttpGet("{idOrSlug}/formulas")]
        public async Task<IActionResult> GetChapterFormulas(string idOrSlug)
        {
            var result = await _curriculumContext.GetFormulasByChapterAsync(idOrSlug);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("{chapterId:guid}/formulas")]
        public async Task<IActionResult> CreateFormula(Guid chapterId, [FromBody] CreateFormulaRequest request)
        {
            var result = await _curriculumContext.CreateFormulaAsync(chapterId, request);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPut("formulas/{id:guid}")]
        public async Task<IActionResult> UpdateFormula(Guid id, [FromBody] UpdateFormulaRequest request)
        {
            var result = await _curriculumContext.UpdateFormulaAsync(id, request);
            return StatusCode(result.StatusCode, result);
        }

        [HttpDelete("formulas/{id:guid}")]
        public async Task<IActionResult> DeleteFormula(Guid id)
        {
            var result = await _curriculumContext.DeleteFormulaAsync(id);
            return StatusCode(result.StatusCode, result);
        }

        public class UpdateIpfsHashRequest
        {
            public string IpfsHash { get; set; } = string.Empty;
        }
    }
}
