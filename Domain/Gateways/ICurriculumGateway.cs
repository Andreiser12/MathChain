using MathChain.Domain.Entities;

namespace MathChain.Domain.Gateways
{
    public interface ICurriculumGateway
    {
        Task<CurriculumChapter?> GetbySlugAsync(string slug);
        Task<List<ChapterCurriculum>> GetAllAsync();
        Task UpdateIpfshashAsync(string slug, string newIpfsHash);
    }
}
