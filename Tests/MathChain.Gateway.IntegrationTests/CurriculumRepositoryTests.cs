using MathChain.Domain.Entities;
using MathChain.Gateway.Repositories;

namespace MathChain.Gateway.IntegrationTests
{
    public class CurriculumRepositoryTests : InMemoryDatabaseTests
    {
        [Fact]
        public async Task ReplacePrerequisitesAsync_ShouldReplaceOnlyTheArcsOfTheChapter_WhenOtherChaptersHavePrerequisites()
        {
            var first = Guid.NewGuid();
            var second = Guid.NewGuid();
            var third = Guid.NewGuid();
            await using (var dbContext = CreateDbContext())
            {
                var repository = new CurriculumRepository(dbContext);
                await repository.ReplacePrerequisitesAsync(third, new[] { first, second, first });
                await repository.ReplacePrerequisitesAsync(second, new[] { first });
            }

            await using (var dbContext = CreateDbContext())
            {
                await new CurriculumRepository(dbContext).ReplacePrerequisitesAsync(third, new[] { second });
            }

            await using (var dbContext = CreateDbContext())
            {
                var stored = await new CurriculumRepository(dbContext).GetPrerequisitesAsync();

                Assert.Equal(2, stored.Count);
                Assert.Contains(stored, p => p.ChapterId == third && p.PrerequisiteId == second);
                Assert.Contains(stored, p => p.ChapterId == second && p.PrerequisiteId == first);
            }
        }

        [Fact]
        public async Task GetFormulasByChapterIdsAsync_ShouldReturnOnlyTheRequestedChapters_WhenSeveralChaptersHaveFormulas()
        {
            var wanted = Guid.NewGuid();
            await using (var dbContext = CreateDbContext())
            {
                var repository = new CurriculumRepository(dbContext);
                await repository.CreateFormulaAsync(new CurriculumFormula { Id = Guid.NewGuid(), ChapterId = wanted, Title = "a", Latex = "a" });
                await repository.CreateFormulaAsync(new CurriculumFormula { Id = Guid.NewGuid(), ChapterId = Guid.NewGuid(), Title = "b", Latex = "b" });
            }

            await using (var dbContext = CreateDbContext())
            {
                var repository = new CurriculumRepository(dbContext);

                Assert.Equal("a", Assert.Single(await repository.GetFormulasByChapterIdsAsync(new[] { wanted })).Title);
                Assert.Empty(await repository.GetFormulasByChapterIdsAsync(Array.Empty<Guid>()));
            }
        }

        [Fact]
        public async Task UpdateAsync_ShouldPersistTheFormulasCountAndIncrementTheVersion_WhenTheChapterIsSaved()
        {
            var chapter = new CurriculumChapter { Id = Guid.NewGuid(), Title = "Integrale", ChapterSlug = "integrale", Category = "Analiza", Version = 1 };
            await using (var dbContext = CreateDbContext())
            {
                await new CurriculumRepository(dbContext).CreateAsync(chapter);
            }

            await using (var dbContext = CreateDbContext())
            {
                var repository = new CurriculumRepository(dbContext);
                var detached = await repository.GetByIdAsync(chapter.Id);
                detached!.FormulasCount = 3;
                await repository.UpdateAsync(detached);
            }

            await using (var dbContext = CreateDbContext())
            {
                var stored = await new CurriculumRepository(dbContext).GetByIdAsync(chapter.Id);
                Assert.Equal(3, stored!.FormulasCount);
                Assert.Equal(2, stored.Version);
            }
        }
    }
}
