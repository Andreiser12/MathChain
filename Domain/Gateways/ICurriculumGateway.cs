using MathChain.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MathChain.Domain.Gateways
{
    public interface ICurriculumGateway
    {
        Task<CurriculumChapter?> GetByIdAsync(Guid id);
        Task<CurriculumChapter?> GetBySlugAsync(string slug);
        Task<List<CurriculumChapter>> GetAllAsync();
        Task<List<CurriculumChapter>> GetRootChaptersAsync();
        Task<List<CurriculumChapter>> GetSubChaptersAsync(Guid parentId);
        Task<List<CurriculumChapter>> GetBreadcrumbsAsync(Guid chapterId);
        Task<CurriculumChapter> CreateAsync(CurriculumChapter chapter);
        Task<CurriculumChapter> UpdateAsync(CurriculumChapter chapter);
        Task<bool> DeleteAsync(Guid id);
        Task UpdateIpfsHashAsync(string slug, string newIpfsHash);

        // Formulas
        Task<List<CurriculumFormula>> GetFormulasByChapterIdAsync(Guid chapterId);
        Task<CurriculumFormula?> GetFormulaByIdAsync(Guid id);
        Task<CurriculumFormula> CreateFormulaAsync(CurriculumFormula formula);
        Task<CurriculumFormula> UpdateFormulaAsync(CurriculumFormula formula);
        Task<bool> DeleteFormulaAsync(Guid id);
    }
}
