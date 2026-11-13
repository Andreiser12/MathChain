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
        private readonly FakeCurrentUser _currentUser = new();
        private readonly UserContext _context;

        public UserContextTests()
        {
            _context = new UserContext(_gateway, new AccessGuard(_currentUser, new InMemoryClassRoomGateway()));
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
        public async Task GetByWalletAddressAsync_OwnUnknownWallet_ThrowsNotFoundException()
        {
            _currentUser.As("0xmissing", UserRole.Student);

            await Assert.ThrowsAsync<NotFoundException>(() => _context.GetByWalletAddressAsync("0xmissing"));
        }

        [Fact]
        public async Task GetByWalletAddressAsync_AnotherUsersWallet_ThrowsForbiddenException()
        {
            AddUser("0xabc");
            _currentUser.As("0xother", UserRole.Teacher);

            await Assert.ThrowsAsync<ForbiddenException>(() => _context.GetByWalletAddressAsync("0xabc"));
        }

        [Fact]
        public async Task GetByWalletAddressAsync_Admin_ReturnsAnyUser()
        {
            var user = AddUser("0xabc");
            _currentUser.As("0xadmin", UserRole.Admin);

            Assert.Same(user, await _context.GetByWalletAddressAsync("0xABC"));
        }

        [Fact]
        public async Task GetAllUsersAsync_NonAdmin_ThrowsForbiddenException()
        {
            _currentUser.As("0xteacher", UserRole.Teacher);

            await Assert.ThrowsAsync<ForbiddenException>(() => _context.GetAllUsersAsync());
        }
    }
}
