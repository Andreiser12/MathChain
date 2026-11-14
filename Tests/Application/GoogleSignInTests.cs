using System.IdentityModel.Tokens.Jwt;
using MathChain.Application.Contexts;
using MathChain.Application.DTOs;
using MathChain.Domain.Entities;
using MathChain.Domain.Enums;
using MathChain.Domain.Exceptions;
using MathChain.Infrastructure.Authentication;
using MathChain.Tests.Fakes;
using Microsoft.Extensions.Caching.Memory;

namespace MathChain.Tests.Application
{
    public class GoogleSignInTests
    {
        private const string Subject = "108204268033311374519";

        private readonly InMemoryUserGateway _users = new();
        private readonly InMemoryExternalLoginGateway _logins;
        private readonly FakeGoogleTokenVerifier _google = new();
        private readonly FakeCustodialAddressProvider _addresses = new();
        private readonly ManualTimeProvider _time = new();
        private readonly AuthenticationContext _context;

        public GoogleSignInTests()
        {
            _logins = new InMemoryExternalLoginGateway(_users);
            _context = new AuthenticationContext(
                _users,
                new MemoryLoginChallengeStore(new MemoryCache(new MemoryCacheOptions()), _time),
                new EthereumSignatureVerifier(),
                new JwtAccessTokenService(new JwtSettings { Key = "test-key-with-at-least-thirty-two-bytes!!" }, _time),
                new SignInSettings(),
                _time,
                _google,
                _logins,
                _addresses);
        }

        private Task<AuthResultDto> SignInAsync(string subject, string? role)
        {
            return _context.SignInWithGoogleAsync(new GoogleSignInRequest { IdToken = _google.IssueToken(subject), Role = role });
        }

        [Theory]
        [InlineData("Student", UserRole.Student)]
        [InlineData("teacher", UserRole.Teacher)]
        public async Task SignInWithGoogleAsync_NewUserWithRole_RegistersUserWithCustodialAddress(string role, UserRole expectedRole)
        {
            var result = await SignInAsync(Subject, role);

            Assert.True(result.IsNewUser);
            var user = Assert.Single(_users.Users);
            Assert.Equal(expectedRole, user.Role);
            Assert.Equal(_addresses.GetOwnerAddress(user.Id), user.WalletAddress);
            Assert.Equal(_time.Now.UtcDateTime, user.CreatedAt);
            var login = Assert.Single(_logins.Logins);
            Assert.Equal(user.Id, login.UserId);
            Assert.Equal(ExternalLogin.GoogleProvider, login.Provider);
            Assert.Equal(Subject, login.Subject);
        }

        [Fact]
        public async Task SignInWithGoogleAsync_NewUser_TokenIdentifiesCustodialAddressAndRole()
        {
            var result = await SignInAsync(Subject, "Teacher");

            var token = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);
            Assert.Equal(result.User.WalletAddress, token.Claims.Single(c => c.Type == WalletClaimTypes.Wallet).Value);
            Assert.Equal("Teacher", token.Claims.Single(c => c.Type == WalletClaimTypes.Role).Value);
            Assert.DoesNotContain(token.Claims, c => c.Value.Contains(Subject));
        }

        [Fact]
        public async Task SignInWithGoogleAsync_NewUserWithoutRole_ThrowsNotFoundAndStoresNothing()
        {
            await Assert.ThrowsAsync<NotFoundException>(() => SignInAsync(Subject, null));

            Assert.Empty(_users.Users);
            Assert.Empty(_logins.Logins);
        }

        [Theory]
        [InlineData("Admin")]
        [InlineData("2")]
        public async Task SignInWithGoogleAsync_NewUserWithForbiddenRole_ThrowsBusinessRuleException(string role)
        {
            await Assert.ThrowsAsync<BusinessRuleException>(() => SignInAsync(Subject, role));

            Assert.Empty(_users.Users);
            Assert.Empty(_logins.Logins);
        }

        [Fact]
        public async Task SignInWithGoogleAsync_ReturningUser_KeepsAddressAndIgnoresRequestedRole()
        {
            var first = await SignInAsync(Subject, "Student");

            var second = await SignInAsync(Subject, "Teacher");

            Assert.False(second.IsNewUser);
            Assert.Equal(first.User.WalletAddress, second.User.WalletAddress);
            Assert.Equal(UserRole.Student, second.User.Role);
            Assert.Single(_users.Users);
            Assert.Single(_logins.Logins);
        }

        [Fact]
        public async Task SignInWithGoogleAsync_ReturningUserWithoutRole_LogsIn()
        {
            await SignInAsync(Subject, "Student");

            var result = await SignInAsync(Subject, null);

            Assert.False(result.IsNewUser);
        }

        [Fact]
        public async Task SignInWithGoogleAsync_DifferentGoogleAccounts_GetDifferentUsers()
        {
            var first = await SignInAsync(Subject, "Student");
            var second = await SignInAsync("999999999999999999999", "Student");

            Assert.NotEqual(first.User.Id, second.User.Id);
            Assert.NotEqual(first.User.WalletAddress, second.User.WalletAddress);
        }

        [Fact]
        public async Task SignInWithGoogleAsync_ConcurrentRegistration_ReturnsTheUserThatWasStored()
        {
            var winner = new User { Id = Guid.NewGuid(), WalletAddress = "0xwinner", Role = UserRole.Teacher };
            _logins.ConcurrentRegistration = (winner, new ExternalLogin { Id = Guid.NewGuid(), UserId = winner.Id, Provider = ExternalLogin.GoogleProvider, Subject = Subject });

            var result = await SignInAsync(Subject, "Student");

            Assert.False(result.IsNewUser);
            Assert.Equal("0xwinner", result.User.WalletAddress);
            Assert.Equal(UserRole.Teacher, result.User.Role);
            Assert.Single(_users.Users);
            Assert.Single(_logins.Logins);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task SignInWithGoogleAsync_BlankToken_ThrowsBusinessRuleException(string idToken)
        {
            await Assert.ThrowsAsync<BusinessRuleException>(() => _context.SignInWithGoogleAsync(new GoogleSignInRequest { IdToken = idToken, Role = "Student" }));
        }

        [Fact]
        public async Task SignInWithGoogleAsync_InvalidToken_ThrowsUnauthorizedAndStoresNothing()
        {
            await Assert.ThrowsAsync<UnauthorizedException>(() => _context.SignInWithGoogleAsync(new GoogleSignInRequest { IdToken = "forged", Role = "Teacher" }));

            Assert.Empty(_users.Users);
        }

        [Fact]
        public async Task SignInWithGoogleAsync_DoesNotAffectWalletUsers()
        {
            _users.Users.Add(new User { Id = Guid.NewGuid(), WalletAddress = "0x52908400098527886e0f7030069857d2e4169ee7", Role = UserRole.Teacher });

            var result = await SignInAsync(Subject, "Student");

            Assert.Equal(2, _users.Users.Count);
            Assert.NotEqual("0x52908400098527886e0f7030069857d2e4169ee7", result.User.WalletAddress);
        }

        [Fact]
        public void GetSignInProviders_ReturnsConfiguredGoogleClientId()
        {
            Assert.Equal(_google.ClientId, _context.GetSignInProviders().GoogleClientId);

            _google.ClientId = null;

            Assert.Null(_context.GetSignInProviders().GoogleClientId);
        }
    }
}
