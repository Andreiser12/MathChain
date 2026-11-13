using MathChain.Domain.Entities;
using MathChain.Gateway.Repositories;
using MathChain.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace MathChain.Tests.Gateway
{
    public class RepositoryTests
    {
        private readonly string _databaseName = Guid.NewGuid().ToString();

        private MathChainDbContext CreateDbContext()
        {
            return new MathChainDbContext(new DbContextOptionsBuilder<MathChainDbContext>()
                .UseInMemoryDatabase(_databaseName)
                .Options);
        }

        [Fact]
        public async Task CurriculumRepository_UpdateAsync_PersistsFormulasCountAndIncrementsVersion()
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

        [Fact]
        public async Task ClassContentRepository_GetCommentsByMessageIdsAsync_ReturnsRequestedCommentsOldestFirst()
        {
            var messageId = Guid.NewGuid();
            await using (var dbContext = CreateDbContext())
            {
                var repository = new ClassContentRepository(dbContext);
                await repository.AddCommentAsync(new MessageComment { MessageId = messageId, Content = "second", CreatedAt = DateTime.UtcNow });
                await repository.AddCommentAsync(new MessageComment { MessageId = messageId, Content = "first", CreatedAt = DateTime.UtcNow.AddMinutes(-5) });
                await repository.AddCommentAsync(new MessageComment { MessageId = Guid.NewGuid(), Content = "other", CreatedAt = DateTime.UtcNow });
            }

            await using (var dbContext = CreateDbContext())
            {
                var comments = await new ClassContentRepository(dbContext).GetCommentsByMessageIdsAsync(new List<Guid> { messageId });

                Assert.Equal(new[] { "first", "second" }, comments.Select(c => c.Content));
            }
        }
    }
}
