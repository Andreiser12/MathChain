using MathChain.Application.Contexts;
using MathChain.Domain.Entities;
using MathChain.Domain.Enums;
using MathChain.Domain.Exceptions;
using MathChain.Tests.Fakes;

namespace MathChain.Tests.Application
{
    public class UserContextTests
    {
        private readonly InMemoryUserGateway _gateway = new();
        private readonly UserContext _context;

        public UserContextTests()
        {
            _context = new UserContext(_gateway);
        }

        private User AddUser(string wallet, UserRole role = UserRole.Student)
        {
            var user = new User { Id = Guid.NewGuid(), WalletAddress = wallet, Role = role, CreatedAt = DateTime.UtcNow };
            _gateway.Users.Add(user);
            return user;
        }

        [Fact]
        public async Task CheckUserExistsAsync_UnknownWallet_ReturnsFalse()
        {
            var exists = await _context.CheckUserExistsAsync("0xnew");

            Assert.False(exists);
        }

        [Fact]
        public async Task CheckUserExistsAsync_KnownWalletWithDifferentCase_ReturnsTrue()
        {
            AddUser("0xabc");

            var exists = await _context.CheckUserExistsAsync("0xABC");

            Assert.True(exists);
        }

        [Fact]
        public async Task GetByWalletAddressAsync_UnknownWallet_ThrowsNotFoundException()
        {
            await Assert.ThrowsAsync<NotFoundException>(() => _context.GetByWalletAddressAsync("0xmissing"));
        }
    }
}
