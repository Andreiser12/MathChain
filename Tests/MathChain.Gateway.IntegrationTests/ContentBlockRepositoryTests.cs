using MathChain.Domain.Entities;
using MathChain.Gateway.Repositories;

namespace MathChain.Gateway.IntegrationTests
{
    public class ContentBlockRepositoryTests : InMemoryDatabaseTests
    {
        [Fact]
        public async Task AddMissingAsync_ShouldStoreEachBlockOnce_WhenTheSameBlockIsAddedAgain()
        {
            var block = new ContentBlock { Cid = "bafkreihdwdcefgh4dqkjv67uzcmw7ojee6xedzdetojuzjevtenxquvyku", Codec = 0x55, Data = Array.Empty<byte>(), Size = 0, CreatedAt = DateTime.UtcNow };
            await using (var dbContext = CreateDbContext())
            {
                var repository = new ContentBlockRepository(dbContext);
                Assert.Equal(1, await repository.AddMissingAsync(new[] { block, block }));
            }

            await using (var dbContext = CreateDbContext())
            {
                var repository = new ContentBlockRepository(dbContext);
                Assert.Equal(0, await repository.AddMissingAsync(new[] { block }));
                Assert.True(await repository.ExistsAsync(block.Cid));
                Assert.Single(await repository.GetManyAsync(new[] { block.Cid, "bafkreimissing" }));
                Assert.Empty(await repository.GetManyAsync(Array.Empty<string>()));
            }
        }

        [Fact]
        public async Task GetReferencesAsync_ShouldListEveryStoredIdentifier_WhenAChapterHasAnIpfsHash()
        {
            var chapterId = Guid.NewGuid();
            await using (var dbContext = CreateDbContext())
            {
                dbContext.CurriculumChapters.Add(new CurriculumChapter { Id = chapterId, Title = "Integrale", ChapterSlug = "integrale_refs", IpfsHash = "QmChapter" });
                dbContext.CurriculumChapters.Add(new CurriculumChapter { Id = Guid.NewGuid(), Title = "Gol", ChapterSlug = "gol_refs", IpfsHash = "" });
                await dbContext.SaveChangesAsync();
            }

            await using (var dbContext = CreateDbContext())
            {
                var references = await new ContentBlockRepository(dbContext).GetReferencesAsync();

                var reference = Assert.Single(references);
                Assert.Equal("CurriculumChapters", reference.Source);
                Assert.Equal(chapterId, reference.OwnerId);
                Assert.Equal("QmChapter", reference.Cid);
            }
        }
    }
}
