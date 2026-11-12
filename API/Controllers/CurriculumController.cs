using MathChain.Application.Contexts;
using MathChain.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace MathChain.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CurriculumController : ApiControllerBase
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
                return await GetRootChapters();
            }

            var chapters = await _curriculumContext.GetAllChaptersAsync();
            return Success(chapters, "Chapters loaded successfully.");
        }

        [HttpGet("roots")]
        public async Task<IActionResult> GetRootChapters()
        {
            var roots = await _curriculumContext.GetRootChaptersAsync();
            return Success(roots, "Root chapters loaded successfully.");
        }

        [HttpGet("{idOrSlug}")]
        public async Task<IActionResult> GetChapterDetail(string idOrSlug)
        {
            var detail = await _curriculumContext.GetChapterDetailAsync(idOrSlug);
            return Success(detail, "Chapter details loaded successfully.");
        }

        [HttpPost]
        public async Task<IActionResult> CreateChapter([FromBody] CreateChapterRequest request)
        {
            var chapter = await _curriculumContext.CreateChapterAsync(request);
            return Success(chapter, "Chapter created successfully.", StatusCodes.Status201Created);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateChapter(Guid id, [FromBody] UpdateChapterRequest request)
        {
            var chapter = await _curriculumContext.UpdateChapterAsync(id, request);
            return Success(chapter, "Chapter updated successfully.");
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteChapter(Guid id)
        {
            var deleted = await _curriculumContext.DeleteChapterAsync(id);
            return Success(deleted, "Chapter and any nested subchapters deleted successfully.");
        }

        [HttpPut("{slug}/ipfs")]
        public async Task<IActionResult> UpdateIpfsHash(string slug, [FromBody] UpdateIpfsHashRequest request)
        {
            var chapter = await _curriculumContext.UpdateIpfsHashAsync(slug, request.IpfsHash);
            return Success(chapter, "The IPFS hash was successfully updated.");
        }

        [HttpGet("{idOrSlug}/formulas")]
        public async Task<IActionResult> GetChapterFormulas(string idOrSlug)
        {
            var formulas = await _curriculumContext.GetFormulasByChapterAsync(idOrSlug);
            return Success(formulas, "Formulas retrieved successfully.");
        }

        [HttpPost("{chapterId:guid}/formulas")]
        public async Task<IActionResult> CreateFormula(Guid chapterId, [FromBody] CreateFormulaRequest request)
        {
            var formula = await _curriculumContext.CreateFormulaAsync(chapterId, request);
            return Success(formula, "Formula created successfully.", StatusCodes.Status201Created);
        }

        [HttpPut("formulas/{id:guid}")]
        public async Task<IActionResult> UpdateFormula(Guid id, [FromBody] UpdateFormulaRequest request)
        {
            var formula = await _curriculumContext.UpdateFormulaAsync(id, request);
            return Success(formula, "Formula updated successfully.");
        }

        [HttpDelete("formulas/{id:guid}")]
        public async Task<IActionResult> DeleteFormula(Guid id)
        {
            var deleted = await _curriculumContext.DeleteFormulaAsync(id);
            return Success(deleted, "Formula deleted successfully.");
        }

        public class UpdateIpfsHashRequest
        {
            public string IpfsHash { get; set; } = string.Empty;
        }
    }
}
