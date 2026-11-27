using MathChain.Domain.Entities;
using MathChain.Domain.Gateways;

namespace MathChain.Tests.Shared.Fakes
{
    public class InMemoryContentBlockGateway : IContentBlockGateway
    {
        public Dictionary<string, ContentBlock> Blocks { get; } = new();
        public List<ContentReference> References { get; } = new();

        public Task<List<ContentBlock>> GetManyAsync(IReadOnlyCollection<string> cids)
        {
            return Task.FromResult(cids.Where(Blocks.ContainsKey).Distinct()
                .Select(c => new ContentBlock { Cid = c, Codec = Blocks[c].Codec, Data = (byte[])Blocks[c].Data.Clone(), Size = Blocks[c].Size, CreatedAt = Blocks[c].CreatedAt })
                .ToList());
        }

        public Task<bool> ExistsAsync(string cid)
        {
            return Task.FromResult(Blocks.ContainsKey(cid));
        }

        public Task<int> AddMissingAsync(IReadOnlyCollection<ContentBlock> blocks)
        {
            var added = 0;

            foreach (var block in blocks)
            {
                if (Blocks.TryAdd(block.Cid, block))
                {
                    added++;
                }
            }

            return Task.FromResult(added);
        }

        public Task<List<ContentReference>> GetReferencesAsync()
        {
            return Task.FromResult(References.ToList());
        }
    }
}
