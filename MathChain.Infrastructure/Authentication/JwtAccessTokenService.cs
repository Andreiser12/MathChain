using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MathChain.Domain.Entities;
using MathChain.Domain.Interfaces;
using Microsoft.IdentityModel.Tokens;

namespace MathChain.Infrastructure.Authentication
{
    public class JwtAccessTokenService : IAccessTokenService
    {
        private readonly JwtSettings _settings;
        private readonly TimeProvider _timeProvider;

        public JwtAccessTokenService(JwtSettings settings, TimeProvider timeProvider)
        {
            _settings = settings;
            _timeProvider = timeProvider;
        }

        public AccessToken CreateToken(User user)
        {
            var issuedAt = _timeProvider.GetUtcNow();
            var expiresAt = issuedAt.AddMinutes(_settings.LifetimeMinutes);

            var claims = new[]
            {
                new Claim(WalletClaimTypes.Wallet, user.WalletAddress.ToLowerInvariant()),
                new Claim(WalletClaimTypes.Role, user.Role.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var credentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key)),
                SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _settings.Issuer,
                audience: _settings.Audience,
                claims: claims,
                notBefore: issuedAt.UtcDateTime,
                expires: expiresAt.UtcDateTime,
                signingCredentials: credentials);

            return new AccessToken
            {
                Value = new JwtSecurityTokenHandler().WriteToken(token),
                ExpiresAt = expiresAt
            };
        }
    }
}
