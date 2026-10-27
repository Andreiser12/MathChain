using MathChain.Application.DTOs;
using MathChain.Domain.Entities;
using MathChain.Domain.Gateways;
using MathChain.Infrastructure.ResponseHandling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MathChain.Application.Contexts
{
    public class CurriculumContext : ICurriculumContext
    {
        private readonly ICurriculumGateway _curriculumGateway;

        public CurriculumContext(ICurriculumGateway curriculumGateway)
        {
            _curriculumGateway = curriculumGateway;
        }

        public async Task<Envelope<List<CurriculumChapterResponseDto>>> GetAllChaptersAsync()
        {
            var chapters = await _curriculumGateway.GetAllAsync();
            var childrenLookup = chapters.ToLookup(c => c.ParentId);

            var dtos = chapters.Select(c => MapToResponseDto(c, childrenLookup)).ToList();
            return EnvelopeExtensions.Success(dtos, "Chapters loaded successfully.");
        }

        public async Task<Envelope<List<CurriculumChapterResponseDto>>> GetRootChaptersAsync()
        {
            var all = await _curriculumGateway.GetAllAsync();
            var roots = all.Where(c => c.ParentId == null).OrderBy(c => c.Order).ThenBy(c => c.Title).ToList();
            var childrenLookup = all.ToLookup(c => c.ParentId);

            var dtos = roots.Select(c => MapToResponseDto(c, childrenLookup)).ToList();
            return EnvelopeExtensions.Success(dtos, "Root chapters loaded successfully.");
        }

        public async Task<Envelope<ChapterDetailResponseDto>> GetChapterDetailAsync(string idOrSlug)
        {
            if (string.IsNullOrWhiteSpace(idOrSlug))
            {
                return EnvelopeExtensions.Failure<ChapterDetailResponseDto>("Chapter identifier is required.", 400);
            }

            CurriculumChapter? chapter;
            if (Guid.TryParse(idOrSlug, out var id))
            {
                chapter = await _curriculumGateway.GetByIdAsync(id);
            }
            else
            {
                chapter = await _curriculumGateway.GetBySlugAsync(idOrSlug);
            }

            if (chapter == null)
            {
                return EnvelopeExtensions.Failure<ChapterDetailResponseDto>($"Chapter '{idOrSlug}' was not found.", 404);
            }

            var subChapters = await _curriculumGateway.GetSubChaptersAsync(chapter.Id);
            var allChapters = await _curriculumGateway.GetAllAsync();
            var childrenLookup = allChapters.ToLookup(c => c.ParentId);

            var breadcrumbs = await _curriculumGateway.GetBreadcrumbsAsync(chapter.Id);

            var detail = new ChapterDetailResponseDto
            {
                Chapter = MapToResponseDto(chapter, childrenLookup),
                SubChapters = subChapters.Select(s => MapToResponseDto(s, childrenLookup)).ToList(),
                Breadcrumbs = breadcrumbs.Select(b => new BreadcrumbDto
                {
                    Id = b.Id,
                    Title = b.Title,
                    Slug = b.ChapterSlug
                }).ToList()
            };

            return EnvelopeExtensions.Success(detail, "Chapter details loaded successfully.");
        }

        public async Task<Envelope<CurriculumChapterResponseDto>> CreateChapterAsync(CreateChapterRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Title))
            {
                return EnvelopeExtensions.Failure<CurriculumChapterResponseDto>("Title is required.", 400);
            }

            string slug;
            if (!string.IsNullOrWhiteSpace(request.ChapterSlug))
            {
                slug = Slugify(request.ChapterSlug);
            }
            else
            {
                slug = Slugify(request.Title);
            }

            // Ensure uniqueness
            var existing = await _curriculumGateway.GetBySlugAsync(slug);
            if (existing != null)
            {
                slug = $"{slug}_{Guid.NewGuid().ToString("N").Substring(0, 6)}";
            }

            string category = request.Category ?? string.Empty;
            if (string.IsNullOrWhiteSpace(category) && request.ParentId.HasValue)
            {
                var parent = await _curriculumGateway.GetByIdAsync(request.ParentId.Value);
                if (parent != null)
                {
                    category = !string.IsNullOrWhiteSpace(parent.Category) ? parent.Category : parent.Title;
                }
            }

            var newChapter = new CurriculumChapter
            {
                Id = Guid.NewGuid(),
                ParentId = request.ParentId,
                Title = request.Title.Trim(),
                ChapterSlug = slug,
                Category = category,
                Description = request.Description?.Trim(),
                ImageUrl = request.ImageUrl?.Trim(),
                IpfsHash = request.IpfsHash?.Trim() ?? string.Empty,
                Order = request.Order,
                FormulasCount = request.FormulasCount,
                Version = 1,
                UpdatedAt = DateTime.UtcNow
            };

            var created = await _curriculumGateway.CreateAsync(newChapter);
            var all = await _curriculumGateway.GetAllAsync();
            var childrenLookup = all.ToLookup(c => c.ParentId);

            return EnvelopeExtensions.Success(MapToResponseDto(created, childrenLookup), "Chapter created successfully.", 201);
        }

        public async Task<Envelope<CurriculumChapterResponseDto>> UpdateChapterAsync(Guid id, UpdateChapterRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Title))
            {
                return EnvelopeExtensions.Failure<CurriculumChapterResponseDto>("Title is required.", 400);
            }

            var chapter = await _curriculumGateway.GetByIdAsync(id);
            if (chapter == null)
            {
                return EnvelopeExtensions.Failure<CurriculumChapterResponseDto>($"Chapter with ID '{id}' was not found.", 404);
            }

            string slug = !string.IsNullOrWhiteSpace(request.ChapterSlug)
                ? Slugify(request.ChapterSlug)
                : chapter.ChapterSlug;

            if (slug != chapter.ChapterSlug)
            {
                var existingWithSlug = await _curriculumGateway.GetBySlugAsync(slug);
                if (existingWithSlug != null && existingWithSlug.Id != id)
                {
                    slug = $"{slug}_{Guid.NewGuid().ToString("N").Substring(0, 6)}";
                }
            }

            chapter.Title = request.Title.Trim();
            chapter.ChapterSlug = slug;
            if (request.Category != null) chapter.Category = request.Category.Trim();
            chapter.Description = request.Description?.Trim();
            chapter.ImageUrl = request.ImageUrl?.Trim();
            if (request.IpfsHash != null) chapter.IpfsHash = request.IpfsHash.Trim();
            chapter.Order = request.Order;
            if (request.FormulasCount > 0)
            {
                chapter.FormulasCount = request.FormulasCount;
            }

            var updated = await _curriculumGateway.UpdateAsync(chapter);
            var all = await _curriculumGateway.GetAllAsync();
            var childrenLookup = all.ToLookup(c => c.ParentId);

            return EnvelopeExtensions.Success(MapToResponseDto(updated, childrenLookup), "Chapter updated successfully.");
        }

        public async Task<Envelope<bool>> DeleteChapterAsync(Guid id)
        {
            var chapter = await _curriculumGateway.GetByIdAsync(id);
            if (chapter == null)
            {
                return EnvelopeExtensions.Failure<bool>($"Chapter with ID '{id}' was not found.", 404);
            }

            var deleted = await _curriculumGateway.DeleteAsync(id);
            return EnvelopeExtensions.Success(deleted, "Chapter and any nested subchapters deleted successfully.");
        }

        public async Task<Envelope<CurriculumChapter>> UpdateIpfsHashAsync(string slug, string newIpfsHash)
        {
            if (string.IsNullOrWhiteSpace(slug) || string.IsNullOrWhiteSpace(newIpfsHash))
            {
                return EnvelopeExtensions.Failure<CurriculumChapter>("The slug and new IPFS hash are required.", 400);
            }

            var chapter = await _curriculumGateway.GetBySlugAsync(slug);
            if (chapter == null)
            {
                return EnvelopeExtensions.Failure<CurriculumChapter>($"Chapter '{slug}' was not found.", 404);
            }

            await _curriculumGateway.UpdateIpfsHashAsync(slug, newIpfsHash);
            var updated = await _curriculumGateway.GetBySlugAsync(slug);
            return EnvelopeExtensions.Success(updated!, "The IPFS hash was successfully updated.");
        }

        private static (int TotalSubs, int TotalFormulas) ComputeDescendantStats(Guid chapterId, ILookup<Guid?, CurriculumChapter> childrenLookup)
        {
            int totalSubs = 0;
            int totalFormulas = 0;

            foreach (var child in childrenLookup[chapterId])
            {
                totalSubs += 1;
                totalFormulas += child.FormulasCount;

                var (childSubs, childFormulas) = ComputeDescendantStats(child.Id, childrenLookup);
                totalSubs += childSubs;
                totalFormulas += childFormulas;
            }

            return (totalSubs, totalFormulas);
        }

        private static CurriculumChapterResponseDto MapToResponseDto(
            CurriculumChapter c,
            ILookup<Guid?, CurriculumChapter> childrenLookup)
        {
            var (totalSubs, descendantFormulas) = ComputeDescendantStats(c.Id, childrenLookup);
            int directSubs = childrenLookup[c.Id].Count();

            return new CurriculumChapterResponseDto
            {
                Id = c.Id,
                ParentId = c.ParentId,
                ChapterSlug = c.ChapterSlug,
                Title = c.Title,
                Category = c.Category,
                Description = c.Description,
                ImageUrl = c.ImageUrl,
                IpfsHash = c.IpfsHash,
                Order = c.Order,
                Version = c.Version,
                UpdatedAt = c.UpdatedAt,
                SubChaptersCount = directSubs,
                TotalSubChaptersCount = totalSubs,
                FormulasCount = c.FormulasCount,
                TotalFormulasCount = c.FormulasCount + descendantFormulas
            };
        }

        private static string Slugify(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            var str = text.ToLowerInvariant().Trim();
            str = Regex.Replace(str, @"[^a-z0-9\s-_]", "");
            str = Regex.Replace(str, @"[\s-]+", "_").Trim('_');
            return str;
        }
    }
}
