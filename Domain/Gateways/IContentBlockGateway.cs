using MathChain.Domain.Entities;

namespace MathChain.Domain.Gateways
{
    public interface IContentBlockGateway
    {
        Task<List<ContentBlock>> GetManyAsync(IReadOnlyCollection<string> cids);
        Task<bool> ExistsAsync(string cid);
        Task<int> AddMissingAsync(IReadOnlyCollection<ContentBlock> blocks);
        Task<List<ContentReference>> GetReferencesAsync();
    }
}
