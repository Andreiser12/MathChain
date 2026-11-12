using MathChain.Application.DTOs;
using MathChain.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MathChain.Application.Contexts
{
    public interface ICurriculumContext
    {
        Task<List<CurriculumChapterResponseDto>> GetAllChaptersAsync();
        Task<List<CurriculumChapterResponseDto>> GetRootChaptersAsync();
        Task<ChapterDetailResponseDto> GetChapterDetailAsync(string idOrSlug);
        Task<CurriculumChapterResponseDto> CreateChapterAsync(CreateChapterRequest request);
        Task<CurriculumChapterResponseDto> UpdateChapterAsync(Guid id, UpdateChapterRequest request);
        Task<bool> DeleteChapterAsync(Guid id);
        Task<CurriculumChapter> UpdateIpfsHashAsync(string slug, string newIpfsHash);

        // Formulas
        Task<List<CurriculumFormulaResponseDto>> GetFormulasByChapterAsync(string idOrSlug);
        Task<CurriculumFormulaResponseDto> CreateFormulaAsync(Guid chapterId, CreateFormulaRequest request);
        Task<CurriculumFormulaResponseDto> UpdateFormulaAsync(Guid id, UpdateFormulaRequest request);
        Task<bool> DeleteFormulaAsync(Guid id);
    }
}
