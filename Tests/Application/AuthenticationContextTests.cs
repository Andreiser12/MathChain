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
                _time,
                new FakeGoogleTokenVerifier(),
                new InMemoryExternalLoginGateway(_users),
                new FakeCustodialAddressProvider());
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
        public async Task CreateChallengeAsync_ShouldReturnASignInWithEthereumMessage_WhenTheAddressIsValid()
        {
            var challenge = await _context.CreateChallengeAsync(new LoginChallengeRequest { WalletAddress = Address.ToLowerInvariant() });

            Assert.StartsWith($"{_signInSettings.Domain} wants you to sign in with your Ethereum account:\n{Address}\n", challenge.Message);
            Assert.Contains($"Chain ID: {_signInSettings.ChainId}\n", challenge.Message);
            Assert.Matches("Nonce: [0-9a-f]{32}\n", challenge.Message);
            Assert.Equal(_time.Now.AddMinutes(_signInSettings.ChallengeLifetimeMinutes), challenge.ExpiresAt);
        }

        [Theory]
        [InlineData(1L)]
        [InlineData(8453L)]
        public async Task CreateChallengeAsync_ShouldUseTheChainIdInTheMessage_WhenTheWalletSendsIt(long chainId)
        {
            var challenge = await _context.CreateChallengeAsync(new LoginChallengeRequest { WalletAddress = Address, ChainId = chainId });

            Assert.Contains($"\nChain ID: {chainId}\n", challenge.Message);
        }

        [Theory]
        [InlineData(0L)]
        [InlineData(-5L)]
        public async Task CreateChallengeAsync_ShouldThrowBusinessRuleException_WhenTheChainIdIsInvalid(long chainId)
        {
            await Assert.ThrowsAsync<BusinessRuleException>(() =>
                _context.CreateChallengeAsync(new LoginChallengeRequest { WalletAddress = Address, ChainId = chainId }));
        }

        [Fact]
        public async Task CreateChallengeAsync_ShouldThrowBusinessRuleException_WhenTheChainIsNotAllowed()
        {
            _signInSettings.AllowedChainIds.AddRange(new[] { 1L, 11155111L });

            await Assert.ThrowsAsync<BusinessRuleException>(() =>
                _context.CreateChallengeAsync(new LoginChallengeRequest { WalletAddress = Address, ChainId = 137 }));

            var allowed = await _context.CreateChallengeAsync(new LoginChallengeRequest { WalletAddress = Address, ChainId = 1 });
            Assert.Contains("\nChain ID: 1\n", allowed.Message);
        }

        [Fact]
        public async Task CreateChallengeAsync_ShouldUseDifferentNonces_WhenTwoChallengesAreRequested()
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
        public async Task CreateChallengeAsync_ShouldThrowBusinessRuleException_WhenTheAddressIsInvalid(string address)
        {
            await Assert.ThrowsAsync<BusinessRuleException>(() =>
                _context.CreateChallengeAsync(new LoginChallengeRequest { WalletAddress = address }));
        }

        [Fact]
        public async Task VerifyAsync_ShouldReturnATokenWithTheWalletAndTheRole_WhenAnExistingUserSignsCorrectly()
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
        public async Task VerifyAsync_ShouldRegisterTheUser_WhenANewUserChoosesAnAllowedRole(string role, UserRole expectedRole)
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
        public async Task VerifyAsync_ShouldThrowBusinessRuleException_WhenANewUserChoosesAForbiddenRole(string role)
        {
            var message = await RequestMessageAsync();

            await Assert.ThrowsAsync<BusinessRuleException>(() =>
                _context.VerifyAsync(new VerifyLoginRequest { WalletAddress = Address, Signature = Sign(message, _wallet), Role = role }));
            Assert.Empty(_users.Users);
        }

        [Fact]
        public async Task VerifyAsync_ShouldThrowNotFoundException_WhenANewUserChoosesNoRole()
        {
            var message = await RequestMessageAsync();

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _context.VerifyAsync(new VerifyLoginRequest { WalletAddress = Address, Signature = Sign(message, _wallet) }));
        }

        [Fact]
        public async Task VerifyAsync_ShouldThrowUnauthorizedException_WhenTheSignatureIsFromAnotherWallet()
        {
            AddExistingUser(UserRole.Admin);
            var message = await RequestMessageAsync();

            await Assert.ThrowsAsync<UnauthorizedException>(() =>
                _context.VerifyAsync(new VerifyLoginRequest { WalletAddress = Address, Signature = Sign(message, _otherWallet) }));
        }

        [Fact]
        public async Task VerifyAsync_ShouldThrowUnauthorizedException_WhenTheSignatureIsMalformed()
        {
            AddExistingUser(UserRole.Student);
            await RequestMessageAsync();

            await Assert.ThrowsAsync<UnauthorizedException>(() =>
                _context.VerifyAsync(new VerifyLoginRequest { WalletAddress = Address, Signature = "0x1234" }));
        }

        [Fact]
        public async Task VerifyAsync_ShouldThrowUnauthorizedException_WhenTheSignatureIsReused()
        {
            AddExistingUser(UserRole.Student);
            var message = await RequestMessageAsync();
            var request = new VerifyLoginRequest { WalletAddress = Address, Signature = Sign(message, _wallet) };
            await _context.VerifyAsync(request);

            await Assert.ThrowsAsync<UnauthorizedException>(() => _context.VerifyAsync(request));
        }

        [Fact]
        public async Task VerifyAsync_ShouldConsumeTheChallenge_WhenTheAttemptFails()
        {
            AddExistingUser(UserRole.Student);
            var message = await RequestMessageAsync();
            await Assert.ThrowsAsync<UnauthorizedException>(() =>
                _context.VerifyAsync(new VerifyLoginRequest { WalletAddress = Address, Signature = Sign(message, _otherWallet) }));

            await Assert.ThrowsAsync<UnauthorizedException>(() =>
                _context.VerifyAsync(new VerifyLoginRequest { WalletAddress = Address, Signature = Sign(message, _wallet) }));
        }

        [Fact]
        public async Task VerifyAsync_ShouldThrowUnauthorizedException_WhenTheChallengeHasExpired()
        {
            AddExistingUser(UserRole.Student);
            var message = await RequestMessageAsync();
            _time.Now = _time.Now.AddMinutes(_signInSettings.ChallengeLifetimeMinutes).AddSeconds(1);

            await Assert.ThrowsAsync<UnauthorizedException>(() =>
                _context.VerifyAsync(new VerifyLoginRequest { WalletAddress = Address, Signature = Sign(message, _wallet) }));
        }

        [Fact]
        public async Task VerifyAsync_ShouldThrowUnauthorizedException_WhenThereIsNoChallenge()
        {
            AddExistingUser(UserRole.Student);

            await Assert.ThrowsAsync<UnauthorizedException>(() =>
                _context.VerifyAsync(new VerifyLoginRequest { WalletAddress = Address, Signature = Sign("anything", _wallet) }));
        }
    }
}
