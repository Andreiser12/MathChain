using System.Text;
using MathChain.Domain.Interfaces;
using MathChain.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace MathChain.Infrastructure.Extensions
{
    public static class AuthenticationExtensions
    {
        private const int MinimumKeySizeInBytes = 32;

        public static IServiceCollection AddWalletAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var jwtSettings = configuration.GetSection("Jwt").Get<JwtSettings>() ?? new JwtSettings();

            if (string.IsNullOrWhiteSpace(jwtSettings.Key) || Encoding.UTF8.GetByteCount(jwtSettings.Key) < MinimumKeySizeInBytes)
            {
                throw new InvalidOperationException(
                    $"Jwt:Key must be configured with at least {MinimumKeySizeInBytes} bytes. " +
                    "Set it with: dotnet user-secrets set \"Jwt:Key\" \"<random value>\" --project API");
            }

            services.AddSingleton(jwtSettings);
            services.TryAddSingleton(TimeProvider.System);
            services.AddMemoryCache();
            services.AddSingleton<ILoginChallengeStore, MemoryLoginChallengeStore>();
            services.AddSingleton<IWalletSignatureVerifier, EthereumSignatureVerifier>();
            services.AddSingleton<IAccessTokenService, JwtAccessTokenService>();

            services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.MapInboundClaims = false;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = jwtSettings.Issuer,
                        ValidateAudience = true,
                        ValidAudience = jwtSettings.Audience,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromSeconds(jwtSettings.ClockSkewSeconds),
                        NameClaimType = WalletClaimTypes.Wallet,
                        RoleClaimType = WalletClaimTypes.Role
                    };
                });

            return services;
        }
    }
}
