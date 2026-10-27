using System;
using System.Linq;
using System.Threading.Tasks;
using MathChain.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Tests
{
    public class UnitTest1
    {
        private const string ConnectionString = "Host=aws-0-eu-central-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.fiamcakagrhhtvsiwpuo;Password=AndreiChain12!";

        [Fact]
        public async Task VerifyCurriculumStructure()
        {
            var options = new DbContextOptionsBuilder<MathChainDbContext>()
                .UseNpgsql(ConnectionString)
                .Options;

            using var db = new MathChainDbContext(options);

            var rootCount = await db.CurriculumChapters.CountAsync(c => c.ParentId == null);
            var formulaCount = await db.CurriculumFormulas.CountAsync();

            Assert.Equal(6, rootCount);
            Assert.True(formulaCount >= 390);
        }
    }
}
