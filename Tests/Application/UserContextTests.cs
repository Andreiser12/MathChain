using MathChain.Application.Contexts;
using MathChain.Application.DTOs;
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
        public async Task LoginOrRegisterAsync_ExistingWallet_ReturnsExistingUser()
        {
            var existing = AddUser("0xabc", UserRole.Teacher);

            var result = await _context.LoginOrRegisterAsync(new LoginRequest { WalletAddress = "0xABC" });

            Assert.False(result.IsNewUser);
            Assert.Same(existing, result.User);
            Assert.Single(_gateway.Users);
        }

        [Fact]
        public async Task LoginOrRegisterAsync_NewWalletWithRole_RegistersUser()
        {
            var result = await _context.LoginOrRegisterAsync(new LoginRequest { WalletAddress = "0xNew", Role = "Student" });

            Assert.True(result.IsNewUser);
            Assert.Equal("0xnew", result.User.WalletAddress);
            Assert.Equal(UserRole.Student, result.User.Role);
            Assert.Contains(result.User, _gateway.Users);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("null")]
        [InlineData("undefined")]
        public async Task LoginOrRegisterAsync_NewWalletWithoutRole_ThrowsNotFoundException(string? role)
        {
            var request = new LoginRequest { WalletAddress = "0xnew", Role = role };

            await Assert.ThrowsAsync<NotFoundException>(() => _context.LoginOrRegisterAsync(request));
            Assert.Empty(_gateway.Users);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task LoginOrRegisterAsync_MissingWallet_ThrowsBusinessRuleException(string wallet)
        {
            var request = new LoginRequest { WalletAddress = wallet, Role = "Student" };

            await Assert.ThrowsAsync<BusinessRuleException>(() => _context.LoginOrRegisterAsync(request));
        }

        [Fact]
        public async Task GetByWalletAddressAsync_UnknownWallet_ThrowsNotFoundException()
        {
            await Assert.ThrowsAsync<NotFoundException>(() => _context.GetByWalletAddressAsync("0xmissing"));
        }
    }
}
