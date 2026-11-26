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
        public async Task CurriculumRepository_ReplacePrerequisitesAsync_ReplacesOnlyTheArcsOfTheChapter()
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
        public async Task CurriculumRepository_GetFormulasByChapterIdsAsync_ReturnsOnlyTheRequestedChapters()
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
        public async Task ExternalLoginRepository_TryCreateUserWithLoginAsync_StoresUserAndLoginTogether()
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
