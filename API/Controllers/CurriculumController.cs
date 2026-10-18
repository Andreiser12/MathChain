using MathChain.API.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MathChain.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CurriculumController : ControllerBase
    {
        private readonly MathChainDbContext _context;

        public CurriculumController(MathChainDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllChapters()
        {
            var chapters = await _context.CurriculumChapters
                .AsNoTracking()
                .OrderBy(c => c.Category)
                .ThenBy(c => c.Title)
                .ToListAsync();

            return Ok(chapters);
        }

        [HttpGet("{slug}")]
        public async Task<IActionResult> GetChapterBySlug(string slug)
        {
            if(string.IsNullOrWhiteSpace(slug))
            {
                return BadRequest("Slug-ul capitolului e obligatoriu.");
            }

            var chapter = await _context.CurriculumChapters
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.ChapterSlug.ToLower() == slug.ToLower());

            if(chapter == null)
            {
                return NotFound($"Capitolul cu slug-ul '{slug}' nu a fost gasit.");
            }

            return Ok(chapter);
        }

        [HttpPut("{slug}/ipfs")]
        public async Task<IActionResult> UpdateIpfsHash(string slug, [FromBody] UpdateIpfsHashRequest request)
        {
            if(string.IsNullOrWhiteSpace(slug) || string.IsNullOrWhiteSpace(request?.IpfsHash))
            {
                return BadRequest("SLug-ul si noul IpfsHash sunt obligatorii.");
            }

            var chapter = await _context.CurriculumChapters
                .FirstOrDefaultAsync(c => c.ChapterSlug.ToLower() == slug.ToLower());

            if(chapter == null)
            {
                return NotFound($"Capitolul cu slug-ul '{slug}' nu a fost gasit.");
            }

            chapter.IpfsHash = request.IpfsHash;
            chapter.Version += 1;
            chapter.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Hash-ul IPFS a fost actualizat cu succes.",
                chapter.ChapterSlug,
                chapter.IpfsHash,
                chapter.Version,
                chapter.UpdatedAt
            });
        }

        public class UpdateIpfsHashRequest
        {
            public string IpfsHash { get; set; } = string.Empty;
        }
    }
}
