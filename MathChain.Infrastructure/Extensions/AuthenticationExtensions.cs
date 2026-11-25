using System.Text;
using MathChain.Blockchain.Custody;
using MathChain.Domain.Interfaces;
using MathChain.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
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
            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUser, HttpContextCurrentUser>();
            services.AddGoogleSignIn(configuration);

            services.AddAuthorization(options =>
            {
                options.FallbackPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
            });

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

        private static void AddGoogleSignIn(this IServiceCollection services, IConfiguration configuration)
        {
            var googleSettings = configuration.GetSection("Google").Get<GoogleAuthSettings>() ?? new GoogleAuthSettings();
            var custodySettings = configuration.GetSection("Custody").Get<CustodySettings>() ?? new CustodySettings();
            var keyDeriver = new CustodialKeyDeriver(custodySettings);

            if (!string.IsNullOrWhiteSpace(googleSettings.ClientId) && !keyDeriver.IsConfigured)
            {
                throw new InvalidOperationException(
                    $"Custody:MasterSecret must be configured with at least {CustodySettings.MinimumSecretSizeInBytes} bytes when Google:ClientId is set. " +
                    "Set it with: dotnet user-secrets set \"Custody:MasterSecret\" \"<random value>\" --project API");
            }

            services.AddSingleton(googleSettings);
            services.AddSingleton<ICustodialAddressProvider>(keyDeriver);
            services.AddSingleton<ICustodialSigner>(keyDeriver);
            services.AddSingleton<IConfigurationManager<OpenIdConnectConfiguration>>(_ =>
                new ConfigurationManager<OpenIdConnectConfiguration>(
                    $"{googleSettings.Authority.TrimEnd('/')}/.well-known/openid-configuration",
                    new OpenIdConnectConfigurationRetriever(),
                    new HttpDocumentRetriever { RequireHttps = true }));
            services.AddSingleton<IGoogleTokenVerifier, GoogleTokenVerifier>();
        }
    }
}
