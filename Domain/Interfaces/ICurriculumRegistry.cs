using MathChain.Domain.Entities;

namespace MathChain.Domain.Interfaces
{
    public interface ICurriculumRegistry
    {
        bool CanRead { get; }
        bool CanPublish { get; }
        CurriculumRegistryInfo Info { get; }
        Task<CurriculumAnchor?> GetLatestAsync();
        Task<CurriculumAnchor> PublishAsync(byte[] cid);
    }
}
