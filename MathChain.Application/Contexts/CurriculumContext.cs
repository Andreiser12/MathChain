using MathChain.Application.Curriculum;
using MathChain.Application.DTOs;
using MathChain.Domain.Entities;
using MathChain.Domain.Enums;
using MathChain.Domain.Gateways;
using MathChain.Domain.Interfaces;
using MathChain.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MathChain.Application.Contexts
{
    public class CurriculumContext : ICurriculumContext
    {
        private readonly ICurriculumGateway _curriculumGateway;
        private readonly IIpfsStorageService? _ipfsStorageService;
        private readonly AccessGuard _access;
        private readonly ChapterUnlockService? _unlocks;

        public CurriculumContext(ICurriculumGateway curriculumGateway, AccessGuard access, IIpfsStorageService? ipfsStorageService = null,
            ChapterUnlockService? unlocks = null)
        {
            _unlocks = unlocks;
            _curriculumGateway = curriculumGateway;
            _ipfsStorageService = ipfsStorageService;
            _access = access;
        }

        public async Task<List<CurriculumChapterResponseDto>> GetAllChaptersAsync()
        {
            var chapters = await _curriculumGateway.GetAllAsync();
            var childrenLookup = chapters.ToLookup(c => c.ParentId);

            var dtos = chapters.Select(c => MapToResponseDto(c, childrenLookup)).ToList();
            return dtos;
        }

        public async Task<List<CurriculumChapterResponseDto>> GetRootChaptersAsync()
        {
            var all = await _curriculumGateway.GetAllAsync();
            var roots = all.Where(c => c.ParentId == null).OrderBy(c => c.Order).ThenBy(c => c.Title).ToList();
            var childrenLookup = all.ToLookup(c => c.ParentId);

            var dtos = roots.Select(c => MapToResponseDto(c, childrenLookup)).ToList();
            return dtos;
        }

        public async Task<ChapterDetailResponseDto> GetChapterDetailAsync(string idOrSlug)
        {
            if (string.IsNullOrWhiteSpace(idOrSlug))
            {
                throw new BusinessRuleException("Chapter identifier is required.");
            }

            var allChapters = await _curriculumGateway.GetAllAsync();
            var chaptersById = allChapters.ToDictionary(c => c.Id);

            CurriculumChapter? chapter = null;
            if (Guid.TryParse(idOrSlug, out var id))
            {
                chaptersById.TryGetValue(id, out chapter);
            }
            else
            {
                var cleanSlug = idOrSlug.Trim().ToLowerInvariant();
                chapter = allChapters.FirstOrDefault(c => c.ChapterSlug.ToLowerInvariant() == cleanSlug);
            }

            if (chapter == null)
            {
                throw new NotFoundException($"Chapter '{idOrSlug}' was not found.");
            }

            var prerequisites = await _curriculumGateway.GetPrerequisitesAsync();
            var subChapters = OrderSiblings(allChapters.Where(c => c.ParentId == chapter.Id).ToList(), prerequisites);

            var childrenLookup = allChapters.ToLookup(c => c.ParentId);

            // Compute breadcrumbs in memory from allChapters - 0 extra network calls!
            var breadcrumbs = new List<CurriculumChapter>();
            var currentId = (Guid?)chapter.Id;
            while (currentId.HasValue && chaptersById.TryGetValue(currentId.Value, out var ancestor))
            {
                breadcrumbs.Insert(0, ancestor);
                currentId = ancestor.ParentId;
            }

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

            var prerequisitesOf = prerequisites.ToLookup(p => p.ChapterId, p => p.PrerequisiteId);

            foreach (var dto in detail.SubChapters.Append(detail.Chapter))
            {
                dto.PrerequisiteIds = prerequisitesOf[dto.Id].ToList();
            }

            if (_unlocks != null && _access.IsInRole(UserRole.Student))
            {
                var statuses = await _unlocks.EvaluateAsync(_access.RequireWallet(),
                    detail.SubChapters.Select(s => s.Id).Append(chapter.Id).ToList(), allChapters, prerequisites);

                foreach (var dto in detail.SubChapters.Append(detail.Chapter))
                {
                    var status = statuses[dto.Id];
                    dto.Locked = status.Locked;
                    dto.Completed = status.Completed;
                    dto.MissingPrerequisites = status.MissingPrerequisites.Select(c => c.Title).ToList();
                }
            }

            return detail;
        }

        public async Task<CurriculumChapterResponseDto> SetPrerequisitesAsync(Guid chapterId, SetChapterPrerequisitesRequest request)
        {
            _access.RequireRole(UserRole.Admin);

            var allChapters = await _curriculumGateway.GetAllAsync();
            var chapter = allChapters.FirstOrDefault(c => c.Id == chapterId)
                ?? throw new NotFoundException("Chapter not found.");

            if (chapter.ParentId == null)
            {
                throw new BusinessRuleException("Main chapters are always unlocked, so they cannot have prerequisites.");
            }

            var requested = (request?.PrerequisiteIds ?? new List<Guid>()).Distinct().ToList();
            var siblings = allChapters.Where(c => c.ParentId == chapter.ParentId).ToList();
            var siblingIds = siblings.Select(c => c.Id).ToHashSet();

            if (requested.Contains(chapterId))
            {
                throw new BusinessRuleException("A chapter cannot be its own prerequisite.");
            }

            if (requested.Any(id => !siblingIds.Contains(id)))
            {
                throw new BusinessRuleException("A prerequisite must be a chapter with the same parent.");
            }

            var prerequisites = (await _curriculumGateway.GetPrerequisitesAsync())
                .Where(p => p.ChapterId != chapterId)
                .Concat(requested.Select(id => new ChapterPrerequisite { ChapterId = chapterId, PrerequisiteId = id }))
                .ToList();
            var sort = SortSiblings(siblings, prerequisites);

            if (sort.HasCycle)
            {
                var titles = allChapters.ToDictionary(c => c.Id, c => c.Title);
                throw new BusinessRuleException($"These prerequisites would create a cycle: {string.Join(" → ", sort.Cycle!.Select(id => titles[id]))}.");
            }

            await _curriculumGateway.ReplacePrerequisitesAsync(chapterId, requested);

            var dto = MapToResponseDto(chapter, allChapters.ToLookup(c => c.ParentId));
            dto.PrerequisiteIds = requested;

            return dto;
        }

        private static List<CurriculumChapter> OrderSiblings(List<CurriculumChapter> siblings, IReadOnlyCollection<ChapterPrerequisite> prerequisites)
        {
            var byOrder = siblings.OrderBy(c => c.Order).ThenBy(c => c.Title).ToList();
            var sort = SortSiblings(byOrder, prerequisites);

            if (sort.HasCycle)
            {
                return byOrder;
            }

            var byId = byOrder.ToDictionary(c => c.Id);

            return sort.Order.Select(id => byId[id]).ToList();
        }

        private static TopologicalSortResult<Guid> SortSiblings(IReadOnlyCollection<CurriculumChapter> siblings, IReadOnlyCollection<ChapterPrerequisite> prerequisites)
        {
            var reversed = siblings.OrderByDescending(c => c.Order).ThenByDescending(c => c.Title).Select(c => c.Id).ToList();
            var position = reversed.Select((id, index) => (id, index)).ToDictionary(p => p.id, p => p.index);
            var dependents = prerequisites
                .Where(p => position.ContainsKey(p.ChapterId) && position.ContainsKey(p.PrerequisiteId))
                .ToLookup(p => p.PrerequisiteId, p => p.ChapterId);

            return TopologicalSorter.TopologicalSort<Guid>(reversed,
                id => dependents[id].Distinct().OrderBy(dependent => position[dependent]).ToList());
        }

        public async Task<CurriculumChapterResponseDto> CreateChapterAsync(CreateChapterRequest request)
        {
            _access.RequireRole(UserRole.Admin);

            if (request == null || string.IsNullOrWhiteSpace(request.Title))
            {
                throw new BusinessRuleException("Title is required.");
            }

            var slug = BuildSlug(!string.IsNullOrWhiteSpace(request.ChapterSlug) ? request.ChapterSlug : request.Title);

            CurriculumChapter? parent = null;
            if (request.ParentId.HasValue)
            {
                parent = await _curriculumGateway.GetByIdAsync(request.ParentId.Value);
                if (parent == null)
                {
                    throw new NotFoundException($"Parent chapter with ID '{request.ParentId.Value}' was not found.");
                }
            }

            // Ensure uniqueness
            var existing = await _curriculumGateway.GetBySlugAsync(slug);
            if (existing != null)
            {
                slug = $"{slug}_{Guid.NewGuid().ToString("N").Substring(0, 6)}";
            }

            string category = request.Category ?? string.Empty;
            if (string.IsNullOrWhiteSpace(category) && parent != null)
            {
                category = !string.IsNullOrWhiteSpace(parent.Category) ? parent.Category : parent.Title;
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

            return MapToResponseDto(created, childrenLookup);
        }

        public async Task<CurriculumChapterResponseDto> UpdateChapterAsync(Guid id, UpdateChapterRequest request)
        {
            _access.RequireRole(UserRole.Admin);

            if (request == null || string.IsNullOrWhiteSpace(request.Title))
            {
                throw new BusinessRuleException("Title is required.");
            }

            var chapter = await _curriculumGateway.GetByIdAsync(id);
            if (chapter == null)
            {
                throw new NotFoundException($"Chapter with ID '{id}' was not found.");
            }

            string slug = !string.IsNullOrWhiteSpace(request.ChapterSlug)
                ? BuildSlug(request.ChapterSlug)
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

            return MapToResponseDto(updated, childrenLookup);
        }

        public async Task<bool> DeleteChapterAsync(Guid id)
        {
            _access.RequireRole(UserRole.Admin);

            var chapter = await _curriculumGateway.GetByIdAsync(id);
            if (chapter == null)
            {
                throw new NotFoundException($"Chapter with ID '{id}' was not found.");
            }

            var deleted = await _curriculumGateway.DeleteAsync(id);
            return deleted;
        }

        public async Task<CurriculumChapter> UpdateIpfsHashAsync(string slug, string newIpfsHash)
        {
            _access.RequireRole(UserRole.Admin);

            if (string.IsNullOrWhiteSpace(slug) || string.IsNullOrWhiteSpace(newIpfsHash))
            {
                throw new BusinessRuleException("The slug and new IPFS hash are required.");
            }

            var chapter = await _curriculumGateway.GetBySlugAsync(slug);
            if (chapter == null)
            {
                throw new NotFoundException($"Chapter '{slug}' was not found.");
            }

            await _curriculumGateway.UpdateIpfsHashAsync(slug, newIpfsHash);
            var updated = await _curriculumGateway.GetBySlugAsync(slug);
            return updated!;
        }

        #region Formulas CRUD & Hybrid Sync

        public async Task<List<CurriculumFormulaResponseDto>> GetFormulasByChapterAsync(string idOrSlug)
        {
            if (string.IsNullOrWhiteSpace(idOrSlug))
            {
                throw new BusinessRuleException("Chapter identifier is required.");
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
                throw new NotFoundException($"Chapter '{idOrSlug}' was not found.");
            }

            var formulas = await _curriculumGateway.GetFormulasByChapterIdAsync(chapter.Id);
            var dtos = formulas.Select(MapFormulaToDto).ToList();
            return dtos;
        }

        public async Task<CurriculumFormulaResponseDto> CreateFormulaAsync(Guid chapterId, CreateFormulaRequest request)
        {
            _access.RequireRole(UserRole.Admin);

            if (request == null || string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Latex))
            {
                throw new BusinessRuleException("Title and LaTeX formula are required.");
            }

            var chapter = await _curriculumGateway.GetByIdAsync(chapterId);
            if (chapter == null)
            {
                throw new NotFoundException($"Chapter with ID '{chapterId}' was not found.");
            }

            var existingFormulas = await _curriculumGateway.GetFormulasByChapterIdAsync(chapterId);
            string code = !string.IsNullOrWhiteSpace(request.FormulaCode)
                ? request.FormulaCode.Trim()
                : $"{chapter.ChapterSlug}-f{existingFormulas.Count + 1}";

            var formula = new CurriculumFormula
            {
                Id = Guid.NewGuid(),
                ChapterId = chapterId,
                FormulaCode = code,
                Title = request.Title.Trim(),
                Latex = request.Latex.Trim(),
                Description = request.Description?.Trim(),
                Requirement = request.Requirement?.Trim(),
                Model3D = request.Model3D?.Trim(),
                ExercisesJson = request.ExercisesJson,
                Order = request.Order > 0 ? request.Order : existingFormulas.Count + 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var created = await _curriculumGateway.CreateFormulaAsync(formula);
            existingFormulas.Add(created);

            chapter.FormulasCount = existingFormulas.Count;
            await SyncChapterToIpfsAsync(chapter, existingFormulas);
            await _curriculumGateway.UpdateAsync(chapter);

            return MapFormulaToDto(created);
        }

        public async Task<CurriculumFormulaResponseDto> UpdateFormulaAsync(Guid id, UpdateFormulaRequest request)
        {
            _access.RequireRole(UserRole.Admin);

            if (request == null || string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Latex))
            {
                throw new BusinessRuleException("Title and LaTeX formula are required.");
            }

            var formula = await _curriculumGateway.GetFormulaByIdAsync(id);
            if (formula == null)
            {
                throw new NotFoundException($"Formula with ID '{id}' was not found.");
            }

            formula.Title = request.Title.Trim();
            formula.Latex = request.Latex.Trim();
            if (!string.IsNullOrWhiteSpace(request.FormulaCode)) formula.FormulaCode = request.FormulaCode.Trim();
            formula.Description = request.Description?.Trim();
            formula.Requirement = request.Requirement?.Trim();
            formula.Model3D = request.Model3D?.Trim();
            if (request.ExercisesJson != null) formula.ExercisesJson = request.ExercisesJson;
            formula.Order = request.Order;
            formula.UpdatedAt = DateTime.UtcNow;

            var updated = await _curriculumGateway.UpdateFormulaAsync(formula);

            var chapter = await _curriculumGateway.GetByIdAsync(formula.ChapterId);
            if (chapter != null)
            {
                var existingFormulas = await _curriculumGateway.GetFormulasByChapterIdAsync(chapter.Id);
                await SyncChapterToIpfsAsync(chapter, existingFormulas);
                await _curriculumGateway.UpdateAsync(chapter);
            }

            return MapFormulaToDto(updated);
        }

        public async Task<bool> DeleteFormulaAsync(Guid id)
        {
            _access.RequireRole(UserRole.Admin);

            var formula = await _curriculumGateway.GetFormulaByIdAsync(id);
            if (formula == null)
            {
                throw new NotFoundException($"Formula with ID '{id}' was not found.");
            }

            var chapterId = formula.ChapterId;
            var deleted = await _curriculumGateway.DeleteFormulaAsync(id);

            if (deleted)
            {
                var chapter = await _curriculumGateway.GetByIdAsync(chapterId);
                if (chapter != null)
                {
                    var existingFormulas = await _curriculumGateway.GetFormulasByChapterIdAsync(chapter.Id);
                    chapter.FormulasCount = existingFormulas.Count;
                    await SyncChapterToIpfsAsync(chapter, existingFormulas);
                    await _curriculumGateway.UpdateAsync(chapter);
                }
            }

            return deleted;
        }

        private async Task SyncChapterToIpfsAsync(CurriculumChapter chapter, List<CurriculumFormula> formulas)
        {
            if (_ipfsStorageService == null) return;
            try
            {
                var payload = new
                {
                    chapterId = chapter.ChapterSlug,
                    title = chapter.Title,
                    category = chapter.Category,
                    version = $"{chapter.Version}.0",
                    description = chapter.Description ?? "",
                    formulas = formulas.OrderBy(f => f.Order).Select(f => new
                    {
                        id = f.FormulaCode,
                        title = f.Title,
                        latex = f.Latex,
                        model3D = f.Model3D ?? "",
                        description = f.Description ?? "",
                        requirement = f.Requirement ?? "",
                        exercises = !string.IsNullOrWhiteSpace(f.ExercisesJson)
                            ? JsonSerializer.Deserialize<object>(f.ExercisesJson)
                            : new object[0]
                    }).ToList()
                };

                var jsonString = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
                var bytes = System.Text.Encoding.UTF8.GetBytes(jsonString);
                var ipfsHash = await _ipfsStorageService.SaveFileAsync(bytes);
                if (!string.IsNullOrWhiteSpace(ipfsHash))
                {
                    chapter.IpfsHash = ipfsHash;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[IPFS Background Sync] Warning: {ex.Message}");
            }
        }

        private static CurriculumFormulaResponseDto MapFormulaToDto(CurriculumFormula f)
        {
            return new CurriculumFormulaResponseDto
            {
                Id = f.Id,
                ChapterId = f.ChapterId,
                FormulaCode = f.FormulaCode,
                Title = f.Title,
                Latex = f.Latex,
                Description = f.Description,
                Requirement = f.Requirement,
                Model3D = f.Model3D,
                ExercisesJson = f.ExercisesJson,
                Order = f.Order,
                UpdatedAt = f.UpdatedAt
            };
        }

        #endregion

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

        private static string BuildSlug(string text)
        {
            var slug = Slugify(text);
            if (string.IsNullOrEmpty(slug))
            {
                throw new BusinessRuleException("The chapter slug must contain at least one letter or digit.");
            }

            return slug;
        }

        private static string Slugify(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            var str = RemoveDiacritics(text.ToLowerInvariant().Trim());
            str = Regex.Replace(str, @"[^a-z0-9\s-_]", "");
            str = Regex.Replace(str, @"[\s-]+", "_").Trim('_');
            return str;
        }

        private static string RemoveDiacritics(string text)
        {
            var decomposed = text.Normalize(System.Text.NormalizationForm.FormD);
            var letters = decomposed.Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark);
            return new string(letters.ToArray()).Normalize(System.Text.NormalizationForm.FormC);
        }
    }
}
