using System.Reflection;
using MathChain.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MathChain.Tests.Persistence
{
    public class MigrationTests
    {
        private static MathChainDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<MathChainDbContext>()
                .UseNpgsql("Host=localhost;Database=tests;Username=tests;Password=tests")
                .Options;

            return new MathChainDbContext(options);
        }

        [Fact]
        public void Model_HasNoChangesMissingFromMigrations()
        {
            using var context = CreateContext();
            var snapshotModel = context.GetService<IMigrationsAssembly>().ModelSnapshot?.Model;
            Assert.NotNull(snapshotModel);

            if (snapshotModel is IMutableModel mutableModel)
            {
                snapshotModel = mutableModel.FinalizeModel();
            }

            snapshotModel = context.GetService<IModelRuntimeInitializer>().Initialize(snapshotModel);

            var differences = context.GetService<IMigrationsModelDiffer>().GetDifferences(
                snapshotModel.GetRelationalModel(),
                context.GetService<IDesignTimeModel>().Model.GetRelationalModel());

            Assert.Empty(differences);
        }

        [Fact]
        public void EveryMigrationClass_IsDiscoverableByEntityFramework()
        {
            var migrationTypes = typeof(MathChainDbContext).Assembly.GetTypes()
                .Where(type => type.IsSubclassOf(typeof(Migration)) && !type.IsAbstract)
                .ToList();

            Assert.NotEmpty(migrationTypes);
            Assert.All(migrationTypes, type => Assert.NotNull(type.GetCustomAttribute<MigrationAttribute>()));
            Assert.All(migrationTypes, type => Assert.Equal(typeof(MathChainDbContext), type.GetCustomAttribute<DbContextAttribute>()?.ContextType));
        }
    }
}
