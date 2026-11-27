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
                Assert.Equal(("CurriculumChapters", chapterId, "QmChapter"), (reference.Source, reference.OwnerId, reference.Cid));
            }
        }

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

        [Fact]
        public async Task TryCreateUserWithLoginAsync_ShouldStoreTheUserAndTheLoginTogether_WhenTheGoogleAccountIsNew()
        {
            var user = new User { Id = Guid.NewGuid(), WalletAddress = "0xc1dae98e6a141323b6fc845fb41f516bad87d356", Role = MathChain.Domain.Enums.UserRole.Teacher, CreatedAt = DateTime.UtcNow };
            var login = new ExternalLogin { Id = Guid.NewGuid(), UserId = user.Id, Provider = ExternalLogin.GoogleProvider, Subject = "108204268033311374519", CreatedAt = DateTime.UtcNow };

            await using (var dbContext = CreateDbContext())
            {
                Assert.True(await new ExternalLoginRepository(dbContext).TryCreateUserWithLoginAsync(user, login));
            }

            await using (var dbContext = CreateDbContext())
            {
                var storedLogin = await new ExternalLoginRepository(dbContext).GetAsync(ExternalLogin.GoogleProvider, "108204268033311374519");
                var storedUser = await new UserRepository(dbContext).GetByIdAsync(storedLogin!.UserId);

                Assert.Equal(user.Id, storedLogin.UserId);
                Assert.Equal(user.WalletAddress, storedUser!.WalletAddress);
                Assert.Equal(MathChain.Domain.Enums.UserRole.Teacher, storedUser.Role);
                Assert.Null(await new ExternalLoginRepository(dbContext).GetAsync(ExternalLogin.GoogleProvider, "another-subject"));
                Assert.Null(await new ExternalLoginRepository(dbContext).GetAsync("github", "108204268033311374519"));
            }
        }

        [Fact]
        public async Task GetCommentsByMessageIdsAsync_ShouldReturnTheRequestedCommentsOldestFirst_WhenTheMessageHasComments()
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
