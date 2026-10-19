using MathChain.Domain.Entities;

namespace MathChain.Domain.Gateways
{
    public interface ICurriculumGateway
    {
        Task<CurriculumChapter?> GetBySlugAsync(string slug);
        Task<List<CurriculumChapter>> GetAllAsync();
        Task UpdateIpfsHashAsync(string slug, string newIpfsHash);
    }
}
