using MathChain.Domain.Entities;
using MathChain.Domain.Gateways;

namespace MathChain.Tests.Fakes
{
    public class InMemoryCurriculumGateway : ICurriculumGateway
    {
        public List<CurriculumChapter> Chapters { get; } = new();
        public List<CurriculumFormula> Formulas { get; } = new();

        public Task<CurriculumChapter?> GetByIdAsync(Guid id)
        {
            return Task.FromResult(EntityCopy.Detached(Chapters.FirstOrDefault(c => c.Id == id)));
        }

        public Task<CurriculumChapter?> GetBySlugAsync(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                return Task.FromResult<CurriculumChapter?>(null);
            }

            var cleanSlug = slug.Trim().ToLower();
            return Task.FromResult(EntityCopy.Detached(Chapters.FirstOrDefault(c => c.ChapterSlug.ToLower() == cleanSlug)));
        }

        public Task<List<CurriculumChapter>> GetAllAsync()
        {
            return Task.FromResult(Chapters.OrderBy(c => c.Order).ThenBy(c => c.Title).Select(c => EntityCopy.Detached(c)!).ToList());
        }

        public Task<List<CurriculumChapter>> GetRootChaptersAsync()
        {
            return Task.FromResult(Chapters.Where(c => c.ParentId == null).OrderBy(c => c.Order).ThenBy(c => c.Title).Select(c => EntityCopy.Detached(c)!).ToList());
        }

        public Task<List<CurriculumChapter>> GetSubChaptersAsync(Guid parentId)
        {
            return Task.FromResult(Chapters.Where(c => c.ParentId == parentId).OrderBy(c => c.Order).ThenBy(c => c.Title).Select(c => EntityCopy.Detached(c)!).ToList());
        }

        public Task<List<CurriculumChapter>> GetBreadcrumbsAsync(Guid chapterId)
        {
            var breadcrumbs = new List<CurriculumChapter>();
            var current = Chapters.FirstOrDefault(c => c.Id == chapterId);

            while (current != null)
            {
                breadcrumbs.Insert(0, EntityCopy.Detached(current)!);
                current = current.ParentId.HasValue ? Chapters.FirstOrDefault(c => c.Id == current.ParentId.Value) : null;
            }

            return Task.FromResult(breadcrumbs);
        }

        public Task<CurriculumChapter> CreateAsync(CurriculumChapter chapter)
        {
            chapter.ChapterSlug = chapter.ChapterSlug.Trim().ToLowerInvariant();
            chapter.UpdatedAt = DateTime.UtcNow;
            Chapters.Add(chapter);
            return Task.FromResult(chapter);
        }

        public Task<CurriculumChapter> UpdateAsync(CurriculumChapter chapter)
        {
            var existing = Chapters.FirstOrDefault(c => c.Id == chapter.Id)
                ?? throw new KeyNotFoundException($"Chapter with ID '{chapter.Id}' was not found.");

            existing.Title = chapter.Title;
            existing.ChapterSlug = chapter.ChapterSlug.Trim().ToLowerInvariant();
            existing.Category = chapter.Category;
            existing.Description = chapter.Description;
            existing.ImageUrl = chapter.ImageUrl;
            existing.IpfsHash = chapter.IpfsHash;
            existing.Order = chapter.Order;
            existing.ParentId = chapter.ParentId;
            existing.FormulasCount = chapter.FormulasCount;
            existing.Version += 1;
            existing.UpdatedAt = DateTime.UtcNow;
            return Task.FromResult(existing);
        }

        public Task<bool> DeleteAsync(Guid id)
        {
            if (Chapters.All(c => c.Id != id))
            {
                return Task.FromResult(false);
            }

            var removedIds = new HashSet<Guid> { id };
            bool foundMore;
            do
            {
                var children = Chapters.Where(c => c.ParentId.HasValue && removedIds.Contains(c.ParentId.Value) && !removedIds.Contains(c.Id)).Select(c => c.Id).ToList();
                foundMore = children.Count > 0;
                removedIds.UnionWith(children);
            } while (foundMore);

            Chapters.RemoveAll(c => removedIds.Contains(c.Id));
            Formulas.RemoveAll(f => removedIds.Contains(f.ChapterId));
            return Task.FromResult(true);
        }

        public Task UpdateIpfsHashAsync(string slug, string newIpfsHash)
        {
            var chapter = Chapters.FirstOrDefault(c => c.ChapterSlug.ToLower() == slug.Trim().ToLower());

            if (chapter != null)
            {
                chapter.IpfsHash = newIpfsHash;
                chapter.UpdatedAt = DateTime.UtcNow;
            }

            return Task.CompletedTask;
        }

        public Task<List<CurriculumFormula>> GetFormulasByChapterIdAsync(Guid chapterId)
        {
            return Task.FromResult(Formulas.Where(f => f.ChapterId == chapterId).OrderBy(f => f.Order).ThenBy(f => f.CreatedAt).Select(f => EntityCopy.Detached(f)!).ToList());
        }

        public Task<CurriculumFormula?> GetFormulaByIdAsync(Guid id)
        {
            return Task.FromResult(EntityCopy.Detached(Formulas.FirstOrDefault(f => f.Id == id)));
        }

        public Task<CurriculumFormula> CreateFormulaAsync(CurriculumFormula formula)
        {
            formula.CreatedAt = DateTime.UtcNow;
            formula.UpdatedAt = DateTime.UtcNow;
            Formulas.Add(formula);
            return Task.FromResult(formula);
        }

        public Task<CurriculumFormula> UpdateFormulaAsync(CurriculumFormula formula)
        {
            var existing = Formulas.FirstOrDefault(f => f.Id == formula.Id)
                ?? throw new KeyNotFoundException($"Formula with ID '{formula.Id}' was not found.");

            existing.FormulaCode = formula.FormulaCode;
            existing.Title = formula.Title;
            existing.Latex = formula.Latex;
            existing.Description = formula.Description;
            existing.Requirement = formula.Requirement;
            existing.Model3D = formula.Model3D;
            existing.ExercisesJson = formula.ExercisesJson;
            existing.Order = formula.Order;
            existing.UpdatedAt = DateTime.UtcNow;
            return Task.FromResult(existing);
        }

        public Task<bool> DeleteFormulaAsync(Guid id)
        {
            return Task.FromResult(Formulas.RemoveAll(f => f.Id == id) > 0);
        }
    }
}
