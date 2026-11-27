using MathChain.Domain.Entities;
using MathChain.Gateway.Repositories;

namespace MathChain.Gateway.IntegrationTests
{
    public class ExternalLoginRepositoryTests : InMemoryDatabaseTests
    {
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
    }
}
