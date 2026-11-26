using MathChain.Domain.Entities;
using MathChain.Domain.Gateways;
using MathChain.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace MathChain.Gateway.Repositories
{
    public class ContentBlockRepository : BaseRepository, IContentBlockGateway
    {
        public ContentBlockRepository(MathChainDbContext context) : base(context)
        {
        }

        public async Task<List<ContentBlock>> GetManyAsync(IReadOnlyCollection<string> cids)
        {
            if (cids.Count == 0)
            {
                return new List<ContentBlock>();
            }

            return await _context.ContentBlocks
                .AsNoTracking()
                .Where(b => cids.Contains(b.Cid))
                .ToListAsync();
        }

        public async Task<bool> ExistsAsync(string cid)
        {
            return await _context.ContentBlocks.AnyAsync(b => b.Cid == cid);
        }

        public async Task<int> AddMissingAsync(IReadOnlyCollection<ContentBlock> blocks)
        {
            var unique = blocks.GroupBy(b => b.Cid).Select(g => g.First()).ToList();

            for (var attempt = 0; ; attempt++)
            {
                var ids = unique.Select(b => b.Cid).ToList();
                var existing = await _context.ContentBlocks
                    .Where(b => ids.Contains(b.Cid))
                    .Select(b => b.Cid)
                    .ToListAsync();
                var missing = unique.Where(b => !existing.Contains(b.Cid)).ToList();

                if (missing.Count == 0)
                {
                    return 0;
                }

                _context.ContentBlocks.AddRange(missing);

                try
                {
                    await _context.SaveChangesAsync();
                    return missing.Count;
                }
                catch (DbUpdateException)
                {
                    foreach (var entry in _context.ChangeTracker.Entries<ContentBlock>().ToList())
                    {
                        entry.State = EntityState.Detached;
                    }

                    if (attempt > 0)
                    {
                        throw;
                    }
                }
                finally
                {
                    foreach (var entry in _context.ChangeTracker.Entries<ContentBlock>().Where(e => e.State == EntityState.Unchanged).ToList())
                    {
                        entry.State = EntityState.Detached;
                    }
                }
            }
        }

        public async Task<List<ContentReference>> GetReferencesAsync()
        {
            var references = new List<ContentReference>();

            references.AddRange(await _context.CurriculumChapters.AsNoTracking()
                .Where(c => c.IpfsHash != null && c.IpfsHash != "")
                .Select(c => new ContentReference("CurriculumChapters", c.Id, c.IpfsHash!))
                .ToListAsync());
            references.AddRange(await _context.ClassMedia.AsNoTracking()
                .Where(m => m.IpfsHash != "")
                .Select(m => new ContentReference("ClassMedia", m.Id, m.IpfsHash))
                .ToListAsync());
            references.AddRange(await _context.ClassResources.AsNoTracking()
                .Where(r => r.IpfsHash != "")
                .Select(r => new ContentReference("ClassResources", r.Id, r.IpfsHash))
                .ToListAsync());
            references.AddRange(await _context.Assignments.OfType<Assignment>().AsNoTracking()
                .Where(a => a.IpfsHash != null && a.IpfsHash != "")
                .Select(a => new ContentReference("Assignments", a.Id, a.IpfsHash!))
                .ToListAsync());
            references.AddRange(await _context.Submissions.AsNoTracking()
                .Where(s => s.IpfsHash != null && s.IpfsHash != "")
                .Select(s => new ContentReference("Submissions", s.Id, s.IpfsHash!))
                .ToListAsync());

            return references;
        }
    }
}
