using MathChain.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace MathChain.Gateway.IntegrationTests
{
    public abstract class InMemoryDatabaseTests
    {
        private readonly string _databaseName = Guid.NewGuid().ToString();

        protected MathChainDbContext CreateDbContext()
        {
            return new MathChainDbContext(new DbContextOptionsBuilder<MathChainDbContext>()
                .UseInMemoryDatabase(_databaseName)
                .Options);
        }
    }
}
