using System.IdentityModel.Tokens.Jwt;
using MathChain.Application.Contexts;
using MathChain.Application.DTOs;
using MathChain.Domain.Entities;
using MathChain.Domain.Enums;
using MathChain.Domain.Exceptions;
using MathChain.Infrastructure.Authentication;
using MathChain.Tests.Fakes;
using Microsoft.Extensions.Caching.Memory;
using Nethereum.Signer;

namespace MathChain.Tests.Application
{
    public class AuthenticationContextTests
    {
        private readonly EthECKey _wallet = EthECKey.GenerateKey();
        private readonly EthECKey _otherWallet = EthECKey.GenerateKey();
        private readonly InMemoryUserGateway _users = new();
        private readonly ManualTimeProvider _time = new();
        private readonly SignInSettings _signInSettings = new();
        private readonly AuthenticationContext _context;

        public AuthenticationContextTests()
        {
            var jwtSettings = new JwtSettings { Key = "test-key-with-at-least-thirty-two-bytes!!" };
            _context = new AuthenticationContext(
                _users,
                new MemoryLoginChallengeStore(new MemoryCache(new MemoryCacheOptions()), _time),
                new EthereumSignatureVerifier(),
                new JwtAccessTokenService(jwtSettings, _time),
                _signInSettings,
                _time);
        }

        private string Address => _wallet.GetPublicAddress();

        private static string Sign(string message, EthECKey key)
        {
            return new EthereumMessageSigner().EncodeUTF8AndSign(message, key);
        }

        private async Task<string> RequestMessageAsync(string? address = null)
        {
            var challenge = await _context.CreateChallengeAsync(new LoginChallengeRequest { WalletAddress = address ?? Address });
            return challenge.Message;
        }

        private void AddExistingUser(UserRole role)
        {
            _users.Users.Add(new User { Id = Guid.NewGuid(), WalletAddress = Address.ToLowerInvariant(), Role = role });
        }

        [Fact]
        public async Task CreateChallengeAsync_ValidAddress_ReturnsSignInWithEthereumMessage()
        {
            var challenge = await _context.CreateChallengeAsync(new LoginChallengeRequest { WalletAddress = Address.ToLowerInvariant() });

            Assert.StartsWith($"{_signInSettings.Domain} wants you to sign in with your Ethereum account:\n{Address}\n", challenge.Message);
            Assert.Contains($"Chain ID: {_signInSettings.ChainId}\n", challenge.Message);
            Assert.Matches("Nonce: [0-9a-f]{32}\n", challenge.Message);
            Assert.Equal(_time.Now.AddMinutes(_signInSettings.ChallengeLifetimeMinutes), challenge.ExpiresAt);
        }

        [Fact]
        public async Task CreateChallengeAsync_TwoRequests_UseDifferentNonces()
        {
            var first = await RequestMessageAsync();
            var second = await RequestMessageAsync();

            Assert.NotEqual(first, second);
        }

        [Theory]
        [InlineData("")]
        [InlineData("0x123")]
        [InlineData("not-an-address")]
        [InlineData("00x52908400098527886E0F7030069857D2E4169EE7")]
        public async Task CreateChallengeAsync_InvalidAddress_ThrowsBusinessRuleException(string address)
        {
            await Assert.ThrowsAsync<BusinessRuleException>(() =>
                _context.CreateChallengeAsync(new LoginChallengeRequest { WalletAddress = address }));
        }

        [Fact]
        public async Task VerifyAsync_ExistingUserWithValidSignature_ReturnsTokenWithWalletAndRole()
        {
            AddExistingUser(UserRole.Teacher);
            var message = await RequestMessageAsync();

            var result = await _context.VerifyAsync(new VerifyLoginRequest { WalletAddress = Address, Signature = Sign(message, _wallet) });

            Assert.False(result.IsNewUser);
            var token = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);
            Assert.Equal(Address.ToLowerInvariant(), token.Claims.Single(c => c.Type == WalletClaimTypes.Wallet).Value);
            Assert.Equal("Teacher", token.Claims.Single(c => c.Type == WalletClaimTypes.Role).Value);
            Assert.Equal(_time.Now.AddMinutes(60), result.ExpiresAt);
        }

        [Theory]
        [InlineData("Student", UserRole.Student)]
        [InlineData("teacher", UserRole.Teacher)]
        public async Task VerifyAsync_NewUserWithAllowedRole_RegistersUser(string role, UserRole expectedRole)
        {
            var message = await RequestMessageAsync();

            var result = await _context.VerifyAsync(new VerifyLoginRequest { WalletAddress = Address, Signature = Sign(message, _wallet), Role = role });

            Assert.True(result.IsNewUser);
            var user = Assert.Single(_users.Users);
            Assert.Equal(Address.ToLowerInvariant(), user.WalletAddress);
            Assert.Equal(expectedRole, user.Role);
        }

        [Theory]
        [InlineData("Admin")]
        [InlineData("2")]
        [InlineData("999")]
        [InlineData("xyz")]
        public async Task VerifyAsync_NewUserWithForbiddenRole_ThrowsBusinessRuleException(string role)
        {
            var message = await RequestMessageAsync();

            await Assert.ThrowsAsync<BusinessRuleException>(() =>
                _context.VerifyAsync(new VerifyLoginRequest { WalletAddress = Address, Signature = Sign(message, _wallet), Role = role }));
            Assert.Empty(_users.Users);
        }

        [Fact]
        public async Task VerifyAsync_NewUserWithoutRole_ThrowsNotFoundException()
        {
            var message = await RequestMessageAsync();

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _context.VerifyAsync(new VerifyLoginRequest { WalletAddress = Address, Signature = Sign(message, _wallet) }));
        }

        [Fact]
        public async Task VerifyAsync_SignatureFromAnotherWallet_ThrowsUnauthorizedException()
        {
            AddExistingUser(UserRole.Admin);
            var message = await RequestMessageAsync();

            await Assert.ThrowsAsync<UnauthorizedException>(() =>
                _context.VerifyAsync(new VerifyLoginRequest { WalletAddress = Address, Signature = Sign(message, _otherWallet) }));
        }

        [Fact]
        public async Task VerifyAsync_MalformedSignature_ThrowsUnauthorizedException()
        {
            AddExistingUser(UserRole.Student);
            await RequestMessageAsync();

            await Assert.ThrowsAsync<UnauthorizedException>(() =>
                _context.VerifyAsync(new VerifyLoginRequest { WalletAddress = Address, Signature = "0x1234" }));
        }

        [Fact]
        public async Task VerifyAsync_ReusedSignature_ThrowsUnauthorizedException()
        {
            AddExistingUser(UserRole.Student);
            var message = await RequestMessageAsync();
            var request = new VerifyLoginRequest { WalletAddress = Address, Signature = Sign(message, _wallet) };
            await _context.VerifyAsync(request);

            await Assert.ThrowsAsync<UnauthorizedException>(() => _context.VerifyAsync(request));
        }

        [Fact]
        public async Task VerifyAsync_FailedAttempt_ConsumesChallenge()
        {
            AddExistingUser(UserRole.Student);
            var message = await RequestMessageAsync();
            await Assert.ThrowsAsync<UnauthorizedException>(() =>
                _context.VerifyAsync(new VerifyLoginRequest { WalletAddress = Address, Signature = Sign(message, _otherWallet) }));

            await Assert.ThrowsAsync<UnauthorizedException>(() =>
                _context.VerifyAsync(new VerifyLoginRequest { WalletAddress = Address, Signature = Sign(message, _wallet) }));
        }

        [Fact]
        public async Task VerifyAsync_ExpiredChallenge_ThrowsUnauthorizedException()
        {
            AddExistingUser(UserRole.Student);
            var message = await RequestMessageAsync();
            _time.Now = _time.Now.AddMinutes(_signInSettings.ChallengeLifetimeMinutes).AddSeconds(1);

            await Assert.ThrowsAsync<UnauthorizedException>(() =>
                _context.VerifyAsync(new VerifyLoginRequest { WalletAddress = Address, Signature = Sign(message, _wallet) }));
        }

        [Fact]
        public async Task VerifyAsync_WithoutChallenge_ThrowsUnauthorizedException()
        {
            AddExistingUser(UserRole.Student);

            await Assert.ThrowsAsync<UnauthorizedException>(() =>
                _context.VerifyAsync(new VerifyLoginRequest { WalletAddress = Address, Signature = Sign("anything", _wallet) }));
        }
    }
}
