using MathChain.Domain.Entities;
using MathChain.Domain.Gateways;
using MathChain.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace MathChain.Gateway.Repositories
{
    public class CurriculumRepository : BaseRepository, ICurriculumGateway
    {
        public CurriculumRepository(MathChainDbContext context) : base(context)
        {
            
        }

        public async Task<List<CurriculumChapter>> GetAllAsync()
        {
            return await _context.CurriculumChapters
                .AsNoTracking()
                .OrderBy(c => c.Category)
                .ThenBy(c => c.Title)
                .ToListAsync();
        }

        public async Task<CurriculumChapter?> GetBySlugAsync(string slug)
        {
            if(string.IsNullOrWhiteSpace(slug))
            {
                return null;
            }

            return await _context.CurriculumChapters
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.ChapterSlug.ToLower() == slug.ToLower());
        }

        public async Task UpdateIpfsHashAsync(string slug, string newIpfsHash)
        {
            var chapter = await _context.CurriculumChapters
                .FirstOrDefaultAsync(c => c.ChapterSlug.ToLower() == slug.ToLower());

            if(chapter != null)
            {
                chapter.IpfsHash = newIpfsHash;
                chapter.Version += 1;
                chapter.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
            }
        }
    }
}
