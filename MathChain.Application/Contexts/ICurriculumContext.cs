using MathChain.Domain.Entities;
using MathChain.Infrastructure.ResponseHandling;

namespace MathChain.Application.Contexts
{
    public interface ICurriculumContext
    {
        Task<Envelope<List<CurriculumChapter>>> GetAllChaptersAsync();
        Task<Envelope<CurriculumChapter>> GetChapterBySlugAsync(string slug);
        Task<Envelope<CurriculumChapter>> UpdateIpfsHashAsync(string slug, string newIpfsHash);
    }
}
