using MathChain.Domain.Entities;
using MathChain.Domain.Gateways;
using MathChain.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
                .OrderBy(c => c.Order)
                .ThenBy(c => c.Title)
                .ToListAsync();
        }

        public async Task<List<CurriculumChapter>> GetRootChaptersAsync()
        {
            return await _context.CurriculumChapters
                .AsNoTracking()
                .Where(c => c.ParentId == null)
                .OrderBy(c => c.Order)
                .ThenBy(c => c.Title)
                .ToListAsync();
        }

        public async Task<List<CurriculumChapter>> GetSubChaptersAsync(Guid parentId)
        {
            return await _context.CurriculumChapters
                .AsNoTracking()
                .Where(c => c.ParentId == parentId)
                .OrderBy(c => c.Order)
                .ThenBy(c => c.Title)
                .ToListAsync();
        }

        public async Task<CurriculumChapter?> GetByIdAsync(Guid id)
        {
            return await _context.CurriculumChapters
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<CurriculumChapter?> GetBySlugAsync(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                return null;
            }

            var cleanSlug = slug.Trim().ToLower();

            return await _context.CurriculumChapters
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.ChapterSlug.ToLower() == cleanSlug);
        }

        public async Task<List<CurriculumChapter>> GetBreadcrumbsAsync(Guid chapterId)
        {
            var breadcrumbs = new List<CurriculumChapter>();
            var currentId = (Guid?)chapterId;

            while (currentId.HasValue)
            {
                var chapter = await _context.CurriculumChapters
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == currentId.Value);

                if (chapter == null)
                {
                    break;
                }

                breadcrumbs.Insert(0, chapter);
                currentId = chapter.ParentId;
            }

            return breadcrumbs;
        }

        public async Task<CurriculumChapter> CreateAsync(CurriculumChapter chapter)
        {
            chapter.ChapterSlug = chapter.ChapterSlug.Trim().ToLowerInvariant();
            chapter.UpdatedAt = DateTime.UtcNow;

            _context.CurriculumChapters.Add(chapter);
            await _context.SaveChangesAsync();
            return chapter;
        }

        public async Task<CurriculumChapter> UpdateAsync(CurriculumChapter chapter)
        {
            var existing = await _context.CurriculumChapters
                .FirstOrDefaultAsync(c => c.Id == chapter.Id);

            if (existing == null)
            {
                throw new KeyNotFoundException($"Chapter with ID '{chapter.Id}' was not found.");
            }

            existing.Title = chapter.Title;
            existing.ChapterSlug = chapter.ChapterSlug.Trim().ToLowerInvariant();
            existing.Category = chapter.Category;
            existing.Description = chapter.Description;
            existing.ImageUrl = chapter.ImageUrl;
            existing.IpfsHash = chapter.IpfsHash;
            existing.Order = chapter.Order;
            existing.ParentId = chapter.ParentId;
            existing.Version += 1;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var chapter = await _context.CurriculumChapters
                .FirstOrDefaultAsync(c => c.Id == id);

            if (chapter == null)
            {
                return false;
            }

            _context.CurriculumChapters.Remove(chapter);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task UpdateIpfsHashAsync(string slug, string newIpfsHash)
        {
            var cleanSlug = slug.Trim().ToLower();

            var chapter = await _context.CurriculumChapters
                .FirstOrDefaultAsync(c => c.ChapterSlug.ToLower() == cleanSlug);

            if (chapter != null)
            {
                chapter.IpfsHash = newIpfsHash;
                chapter.Version += 1;
                chapter.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
            }
        }
    }
}
