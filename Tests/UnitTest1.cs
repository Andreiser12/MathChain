using System;
using System.Linq;
using System.Threading.Tasks;
using MathChain.Application.Contexts;
using MathChain.Gateway.Repositories;
using MathChain.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Tests
{
    public class UnitTest1
    {
        private const string ConnectionString = "Host=aws-0-eu-central-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.fiamcakagrhhtvsiwpuo;Password=AndreiChain12!";

        [Fact]
        public async Task TestRootChaptersStatistics()
        {
            var options = new DbContextOptionsBuilder<MathChainDbContext>()
                .UseNpgsql(ConnectionString)
                .Options;

            using var db = new MathChainDbContext(options);
            var gateway = new CurriculumRepository(db);
            var context = new CurriculumContext(gateway);

            var result = await context.GetRootChaptersAsync();
            Assert.True(result.Success);
            Assert.NotNull(result.Data);

            Console.WriteLine($"--- ROOT CHAPTERS ({result.Data.Count}) ---");
            foreach (var ch in result.Data)
            {
                Console.WriteLine($"Chapter: {ch.Title} | DirectSubs: {ch.SubChaptersCount} | TotalSubs: {ch.TotalSubChaptersCount} | DirectFormulas: {ch.FormulasCount} | TotalFormulas: {ch.TotalFormulasCount}");
            }
        }
    }
}
