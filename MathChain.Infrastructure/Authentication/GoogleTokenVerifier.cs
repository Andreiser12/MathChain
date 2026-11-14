using MathChain.Domain.Entities;
using MathChain.Domain.Exceptions;
using MathChain.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace MathChain.Infrastructure.Authentication
{
    public class GoogleTokenVerifier : IGoogleTokenVerifier
    {
        private const string InvalidTokenMessage = "Invalid Google sign-in token.";

        private readonly GoogleAuthSettings _settings;
        private readonly IConfigurationManager<OpenIdConnectConfiguration> _configurationManager;
        private readonly TimeProvider _timeProvider;
        private readonly ILogger<GoogleTokenVerifier> _logger;
        private readonly JsonWebTokenHandler _tokenHandler = new() { MapInboundClaims = false };

        public GoogleTokenVerifier(GoogleAuthSettings settings,
            IConfigurationManager<OpenIdConnectConfiguration> configurationManager,
            TimeProvider timeProvider, ILogger<GoogleTokenVerifier> logger)
        {
            _settings = settings;
            _configurationManager = configurationManager;
            _timeProvider = timeProvider;
            _logger = logger;
        }

        public string? ClientId => string.IsNullOrWhiteSpace(_settings.ClientId) ? null : _settings.ClientId.Trim();

        public async Task<GoogleIdentity> VerifyAsync(string idToken)
        {
            if (ClientId == null)
            {
                throw new BusinessRuleException("Google sign-in is not configured on this server.");
            }

            var result = await ValidateAsync(idToken);

            if (!result.IsValid && result.Exception is SecurityTokenSignatureKeyNotFoundException)
            {
                _configurationManager.RequestRefresh();
                result = await ValidateAsync(idToken);
            }

            if (!result.IsValid || result.SecurityToken is not JsonWebToken token)
            {
                _logger.LogWarning("Google ID token rejected: {Reason}", result.Exception?.GetType().Name ?? "unknown");
                throw new UnauthorizedException(InvalidTokenMessage);
            }

            if (string.IsNullOrWhiteSpace(token.Subject) || !WasIssuedRecently(token))
            {
                _logger.LogWarning("Google ID token rejected: missing subject or issued too long ago");
                throw new UnauthorizedException(InvalidTokenMessage);
            }

            return new GoogleIdentity { Subject = token.Subject };
        }

        private async Task<TokenValidationResult> ValidateAsync(string idToken)
        {
            OpenIdConnectConfiguration configuration;

            try
            {
                configuration = await _configurationManager.GetConfigurationAsync(CancellationToken.None);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Could not load the Google signing keys");
                throw new ServiceUnavailableException("Google sign-in is temporarily unavailable. Try again later.");
            }

            var parameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuers = GetValidIssuers(configuration.Issuer),
                ValidateAudience = true,
                ValidAudience = ClientId,
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = configuration.SigningKeys,
                TryAllIssuerSigningKeys = false,
                RequireSignedTokens = true,
                ValidAlgorithms = new[] { SecurityAlgorithms.RsaSha256 },
                RequireExpirationTime = true,
                ValidateLifetime = true,
                LifetimeValidator = IsWithinLifetime
            };

            try
            {
                return await _tokenHandler.ValidateTokenAsync(idToken, parameters);
            }
            catch (Exception exception) when (exception is ArgumentException or SecurityTokenException)
            {
                return new TokenValidationResult { IsValid = false, Exception = exception };
            }
        }

        private static string[] GetValidIssuers(string? issuer)
        {
            if (string.IsNullOrWhiteSpace(issuer))
            {
                return Array.Empty<string>();
            }

            var separator = issuer.IndexOf("://", StringComparison.Ordinal);

            return separator < 0
                ? new[] { issuer }
                : new[] { issuer, issuer[(separator + 3)..] };
        }

        private bool IsWithinLifetime(DateTime? notBefore, DateTime? expires, SecurityToken token, TokenValidationParameters parameters)
        {
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var skew = TimeSpan.FromSeconds(_settings.ClockSkewSeconds);

            if (expires == null || now - skew > expires.Value)
            {
                return false;
            }

            return notBefore == null || now + skew >= notBefore.Value;
        }

        private bool WasIssuedRecently(JsonWebToken token)
        {
            if (token.IssuedAt == DateTime.MinValue)
            {
                return false;
            }

            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var skew = TimeSpan.FromSeconds(_settings.ClockSkewSeconds);
            var age = now - token.IssuedAt;

            return age <= TimeSpan.FromMinutes(_settings.MaxTokenAgeMinutes) + skew && age >= -skew;
        }
    }
}
