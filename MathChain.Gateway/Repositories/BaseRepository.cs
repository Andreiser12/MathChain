using MathChain.Persistence.Context;

namespace MathChain.Gateway.Repositories
{
    public abstract class BaseRepository
    {
        protected readonly MathChainDbContext _context;

        protected BaseRepository(MathChainDbContext context)
        {
            _context = context;
        }
    }
}
