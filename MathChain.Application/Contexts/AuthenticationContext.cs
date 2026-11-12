using System.Globalization;
using System.Security.Cryptography;
using MathChain.Application.DTOs;
using MathChain.Domain.Entities;
using MathChain.Domain.Enums;
using MathChain.Domain.Exceptions;
using MathChain.Domain.Gateways;
using MathChain.Domain.Interfaces;

namespace MathChain.Application.Contexts
{
    public class AuthenticationContext : IAuthenticationContext
    {
        private const int NonceSizeInBytes = 16;
        private const string TimestampFormat = "yyyy-MM-dd'T'HH:mm:ss.fff'Z'";

        private readonly IUserGateway _userGateway;
        private readonly ILoginChallengeStore _challengeStore;
        private readonly IWalletSignatureVerifier _signatureVerifier;
        private readonly IAccessTokenService _tokenService;
        private readonly SignInSettings _settings;
        private readonly TimeProvider _timeProvider;

        public AuthenticationContext(IUserGateway userGateway, ILoginChallengeStore challengeStore,
            IWalletSignatureVerifier signatureVerifier, IAccessTokenService tokenService,
            SignInSettings settings, TimeProvider timeProvider)
        {
            _userGateway = userGateway;
            _challengeStore = challengeStore;
            _signatureVerifier = signatureVerifier;
            _tokenService = tokenService;
            _settings = settings;
            _timeProvider = timeProvider;
        }

        public Task<LoginChallengeDto> CreateChallengeAsync(LoginChallengeRequest request)
        {
            var wallet = NormalizeWallet(request?.WalletAddress);

            var issuedAt = _timeProvider.GetUtcNow();
            var expiresAt = issuedAt.AddMinutes(_settings.ChallengeLifetimeMinutes);
            var nonce = Convert.ToHexString(RandomNumberGenerator.GetBytes(NonceSizeInBytes)).ToLowerInvariant();

            var message =
                $"{_settings.Domain} wants you to sign in with your Ethereum account:\n" +
                $"{_signatureVerifier.ToChecksumAddress(wallet)}\n" +
                "\n" +
                "Sign in to MathChain.\n" +
                "\n" +
                $"URI: {_settings.Uri}\n" +
                "Version: 1\n" +
                $"Chain ID: {_settings.ChainId}\n" +
                $"Nonce: {nonce}\n" +
                $"Issued At: {FormatTimestamp(issuedAt)}\n" +
                $"Expiration Time: {FormatTimestamp(expiresAt)}";

            _challengeStore.Save(new LoginChallenge
            {
                WalletAddress = wallet,
                Message = message,
                ExpiresAt = expiresAt
            });

            return Task.FromResult(new LoginChallengeDto { Message = message, ExpiresAt = expiresAt });
        }

        public async Task<AuthResultDto> VerifyAsync(VerifyLoginRequest request)
        {
            var wallet = NormalizeWallet(request?.WalletAddress);

            if (string.IsNullOrWhiteSpace(request!.Signature))
            {
                throw new BusinessRuleException("Signature is required.");
            }

            var challenge = _challengeStore.Take(wallet);

            if (challenge == null)
            {
                throw new UnauthorizedException("Login challenge not found or already used. Request a new one.");
            }

            if (_timeProvider.GetUtcNow() > challenge.ExpiresAt)
            {
                throw new UnauthorizedException("Login challenge has expired. Request a new one.");
            }

            if (!_signatureVerifier.IsSignedBy(challenge.Message, request.Signature.Trim(), wallet))
            {
                throw new UnauthorizedException("Invalid wallet signature.");
            }

            var user = await _userGateway.GetByWalletAddressAsync(wallet);
            var isNewUser = false;

            if (user == null)
            {
                var role = ParseRegistrationRole(request.Role);

                if (role == null)
                {
                    throw new NotFoundException("User does not exist. A valid role is required for registration.");
                }

                user = await _userGateway.CreateOrUpdateAsync(new User
                {
                    Id = Guid.NewGuid(),
                    WalletAddress = wallet,
                    Role = role.Value,
                    CreatedAt = _timeProvider.GetUtcNow().UtcDateTime
                });
                isNewUser = true;
            }

            var token = _tokenService.CreateToken(user);

            return new AuthResultDto
            {
                Token = token.Value,
                ExpiresAt = token.ExpiresAt,
                User = user,
                IsNewUser = isNewUser
            };
        }

        private string NormalizeWallet(string? walletAddress)
        {
            if (string.IsNullOrWhiteSpace(walletAddress))
            {
                throw new BusinessRuleException("Wallet address is required.");
            }

            var wallet = walletAddress.Trim().ToLowerInvariant();

            if (!_signatureVerifier.IsValidAddress(wallet))
            {
                throw new BusinessRuleException("Wallet address is not a valid Ethereum address.");
            }

            return wallet;
        }

        private static UserRole? ParseRegistrationRole(string? role)
        {
            if (string.IsNullOrWhiteSpace(role)
                || role.Equals("null", StringComparison.OrdinalIgnoreCase)
                || role.Equals("undefined", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (role.Trim().Equals(nameof(UserRole.Student), StringComparison.OrdinalIgnoreCase))
            {
                return UserRole.Student;
            }

            if (role.Trim().Equals(nameof(UserRole.Teacher), StringComparison.OrdinalIgnoreCase))
            {
                return UserRole.Teacher;
            }

            throw new BusinessRuleException("Role must be Student or Teacher.");
        }

        private static string FormatTimestamp(DateTimeOffset value)
        {
            return value.UtcDateTime.ToString(TimestampFormat, CultureInfo.InvariantCulture);
        }
    }
}
