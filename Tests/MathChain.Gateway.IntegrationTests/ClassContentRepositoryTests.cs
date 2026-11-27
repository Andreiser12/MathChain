using MathChain.Domain.Entities;
using MathChain.Gateway.Repositories;

namespace MathChain.Gateway.IntegrationTests
{
    public class ClassContentRepositoryTests : InMemoryDatabaseTests
    {
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
