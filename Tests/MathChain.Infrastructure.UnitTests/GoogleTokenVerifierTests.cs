using MathChain.Domain.Exceptions;
using MathChain.Infrastructure.Authentication;
using MathChain.Tests.Shared.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace MathChain.Infrastructure.UnitTests
{
    public class GoogleTokenVerifierTests
    {
        private const string Subject = "108204268033311374519";

        private readonly GoogleTokenFactory _google = new();
        private readonly ManualTimeProvider _time = new();
        private readonly GoogleAuthSettings _settings = new() { ClientId = GoogleTokenFactory.ClientId };

        private GoogleTokenVerifier CreateVerifier(IConfigurationManager<OpenIdConnectConfiguration>? configurationManager = null)
        {
            return new GoogleTokenVerifier(
                _settings,
                configurationManager ?? new StaticConfigurationManager<OpenIdConnectConfiguration>(_google.CreateConfiguration()),
                _time,
                NullLogger<GoogleTokenVerifier>.Instance);
        }

        private Dictionary<string, object> Claims()
        {
            return _google.ValidClaims(Subject, _time.Now);
        }

        private Task AssertRejectedAsync(string token)
        {
            return Assert.ThrowsAsync<UnauthorizedException>(() => CreateVerifier().VerifyAsync(token));
        }

        [Fact]
        public async Task VerifyAsync_ShouldReturnTheSubject_WhenTheTokenIsValid()
        {
            var identity = await CreateVerifier().VerifyAsync(_google.CreateToken(Subject, _time.Now));

            Assert.Equal(Subject, identity.Subject);
        }

        [Fact]
        public async Task VerifyAsync_ShouldAcceptTheToken_WhenTheIssuerHasNoScheme()
        {
            var claims = Claims();
            claims["iss"] = "accounts.google.com";

            var identity = await CreateVerifier().VerifyAsync(_google.CreateToken(claims));

            Assert.Equal(Subject, identity.Subject);
        }

        [Fact]
        public async Task VerifyAsync_ShouldRejectTheToken_WhenItWasIssuedForAnotherApplication()
        {
            var claims = Claims();
            claims["aud"] = "another-app.apps.googleusercontent.com";

            await AssertRejectedAsync(_google.CreateToken(claims));
        }

        [Fact]
        public async Task VerifyAsync_ShouldRejectTheToken_WhenItComesFromAnotherIssuer()
        {
            var claims = Claims();
            claims["iss"] = "https://accounts.evil.example";

            await AssertRejectedAsync(_google.CreateToken(claims));
        }

        [Fact]
        public async Task VerifyAsync_ShouldRejectTheToken_WhenItWasRecentlyIssuedButHasExpired()
        {
            var claims = Claims();
            claims["iat"] = _time.Now.AddMinutes(-5).ToUnixTimeSeconds();
            claims["exp"] = _time.Now.AddSeconds(-(_settings.ClockSkewSeconds + 1)).ToUnixTimeSeconds();

            await AssertRejectedAsync(_google.CreateToken(claims));
        }

        [Fact]
        public async Task VerifyAsync_ShouldAcceptTheToken_WhenItExpiredWithinTheClockSkew()
        {
            var claims = Claims();
            claims["iat"] = _time.Now.AddMinutes(-5).ToUnixTimeSeconds();
            claims["exp"] = _time.Now.AddSeconds(-(_settings.ClockSkewSeconds - 1)).ToUnixTimeSeconds();

            Assert.Equal(Subject, (await CreateVerifier().VerifyAsync(_google.CreateToken(claims))).Subject);
        }

        [Fact]
        public async Task VerifyAsync_ShouldRejectTheToken_WhenItIsNotValidYet()
        {
            var claims = Claims();
            claims["nbf"] = _time.Now.AddSeconds(_settings.ClockSkewSeconds + 1).ToUnixTimeSeconds();

            await AssertRejectedAsync(_google.CreateToken(claims));
        }

        [Fact]
        public async Task VerifyAsync_ShouldRejectTheToken_WhenItWasIssuedTooLongAgo()
        {
            var token = _google.CreateToken(Subject, _time.Now);
            _time.Now = _time.Now.AddMinutes(_settings.MaxTokenAgeMinutes).AddSeconds(_settings.ClockSkewSeconds + 1);

            await AssertRejectedAsync(token);
        }

        [Fact]
        public async Task VerifyAsync_ShouldAcceptTheToken_WhenItIsWithinTheMaximumAge()
        {
            var token = _google.CreateToken(Subject, _time.Now);
            _time.Now = _time.Now.AddMinutes(_settings.MaxTokenAgeMinutes);

            Assert.Equal(Subject, (await CreateVerifier().VerifyAsync(token)).Subject);
        }

        [Fact]
        public async Task VerifyAsync_ShouldRejectTheToken_WhenItIsIssuedInTheFuture()
        {
            await AssertRejectedAsync(_google.CreateToken(Subject, _time.Now.AddMinutes(5)));
        }

        [Fact]
        public async Task VerifyAsync_ShouldRejectTheToken_WhenItIsSignedByAnotherKey()
        {
            var attacker = new GoogleTokenFactory();

            await AssertRejectedAsync(attacker.CreateToken(Subject, _time.Now));
        }

        [Fact]
        public async Task VerifyAsync_ShouldRejectTheToken_WhenTheKeyIdIsUnknown()
        {
            await AssertRejectedAsync(_google.CreateToken(Claims(), "unknown-key"));
        }

        [Fact]
        public async Task VerifyAsync_ShouldRefreshTheKeysOnceAndRejectTheToken_WhenItIsSignedByAnUnknownKey()
        {
            var attacker = new GoogleTokenFactory("attacker-key");
            var manager = new RotatingConfigurationManager(() => _google.CreateConfiguration());

            await Assert.ThrowsAsync<UnauthorizedException>(() => CreateVerifier(manager).VerifyAsync(attacker.CreateToken(Subject, _time.Now)));
            Assert.Equal(1, manager.RefreshRequests);
        }

        [Fact]
        public async Task VerifyAsync_ShouldRejectTheToken_WhenItIsUnsigned()
        {
            await AssertRejectedAsync(_google.CreateUnsignedToken(Claims()));
        }

        [Fact]
        public async Task VerifyAsync_ShouldRejectTheToken_WhenThePublicKeyIsUsedAsAnHmacSecret()
        {
            await AssertRejectedAsync(_google.CreateTokenSignedWithPublicKeyAsSecret(Claims()));
        }

        [Fact]
        public async Task VerifyAsync_ShouldRejectTheToken_WhenThePayloadIsTampered()
        {
            var parts = _google.CreateToken(Subject, _time.Now).Split('.');
            var otherClaims = Claims();
            otherClaims["sub"] = "someone-else";
            var otherPayload = _google.CreateToken(otherClaims).Split('.')[1];

            await AssertRejectedAsync($"{parts[0]}.{otherPayload}.{parts[2]}");
        }

        [Theory]
        [InlineData("sub")]
        [InlineData("iat")]
        [InlineData("exp")]
        public async Task VerifyAsync_ShouldRejectTheToken_WhenARequiredClaimIsMissing(string claim)
        {
            var claims = Claims();
            claims.Remove(claim);

            await AssertRejectedAsync(_google.CreateToken(claims));
        }

        [Theory]
        [InlineData("not-a-token")]
        [InlineData("a.b.c")]
        [InlineData("....")]
        public async Task VerifyAsync_ShouldRejectTheToken_WhenItIsMalformed(string token)
        {
            await AssertRejectedAsync(token);
        }

        [Fact]
        public async Task VerifyAsync_ShouldThrowBusinessRuleException_WhenThereIsNoClientId()
        {
            _settings.ClientId = "  ";
            var verifier = CreateVerifier();

            Assert.Null(verifier.ClientId);
            await Assert.ThrowsAsync<BusinessRuleException>(() => verifier.VerifyAsync(_google.CreateToken(Subject, _time.Now)));
        }

        [Fact]
        public async Task VerifyAsync_ShouldThrowServiceUnavailableException_WhenTheSigningKeysAreUnavailable()
        {
            var verifier = CreateVerifier(new RotatingConfigurationManager(() => throw new InvalidOperationException("network down")));

            await Assert.ThrowsAsync<ServiceUnavailableException>(() => verifier.VerifyAsync(_google.CreateToken(Subject, _time.Now)));
        }

        [Fact]
        public async Task VerifyAsync_ShouldRefreshTheKeysAndAcceptTheToken_WhenGoogleRotatedTheKey()
        {
            var rotatedGoogle = new GoogleTokenFactory("rotated-key");
            var manager = new RotatingConfigurationManager(() => _google.CreateConfiguration(), () => rotatedGoogle.CreateConfiguration());

            var identity = await CreateVerifier(manager).VerifyAsync(rotatedGoogle.CreateToken(Subject, _time.Now));

            Assert.Equal(Subject, identity.Subject);
            Assert.Equal(1, manager.RefreshRequests);
        }

        private sealed class RotatingConfigurationManager : IConfigurationManager<OpenIdConnectConfiguration>
        {
            private readonly Func<OpenIdConnectConfiguration>[] _configurations;

            public RotatingConfigurationManager(params Func<OpenIdConnectConfiguration>[] configurations)
            {
                _configurations = configurations;
            }

            public int RefreshRequests { get; private set; }

            public Task<OpenIdConnectConfiguration> GetConfigurationAsync(CancellationToken cancel)
            {
                return Task.FromResult(_configurations[Math.Min(RefreshRequests, _configurations.Length - 1)]());
            }

            public void RequestRefresh()
            {
                RefreshRequests++;
            }
        }
    }
}
