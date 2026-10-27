using MathChain.Application.DTOs;
using MathChain.Domain.Entities;
using MathChain.Infrastructure.ResponseHandling;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MathChain.Application.Contexts
{
    public interface ICurriculumContext
    {
        Task<Envelope<List<CurriculumChapterResponseDto>>> GetAllChaptersAsync();
        Task<Envelope<List<CurriculumChapterResponseDto>>> GetRootChaptersAsync();
        Task<Envelope<ChapterDetailResponseDto>> GetChapterDetailAsync(string idOrSlug);
        Task<Envelope<CurriculumChapterResponseDto>> CreateChapterAsync(CreateChapterRequest request);
        Task<Envelope<CurriculumChapterResponseDto>> UpdateChapterAsync(Guid id, UpdateChapterRequest request);
        Task<Envelope<bool>> DeleteChapterAsync(Guid id);
        Task<Envelope<CurriculumChapter>> UpdateIpfsHashAsync(string slug, string newIpfsHash);

        // Formulas
        Task<Envelope<List<CurriculumFormulaResponseDto>>> GetFormulasByChapterAsync(string idOrSlug);
        Task<Envelope<CurriculumFormulaResponseDto>> CreateFormulaAsync(Guid chapterId, CreateFormulaRequest request);
        Task<Envelope<CurriculumFormulaResponseDto>> UpdateFormulaAsync(Guid id, UpdateFormulaRequest request);
        Task<Envelope<bool>> DeleteFormulaAsync(Guid id);
    }
}
