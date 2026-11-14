using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace MathChain.Tests.Fakes
{
    public class GoogleTokenFactory
    {
        public const string Issuer = "https://accounts.google.com";
        public const string ClientId = "test-client-id.apps.googleusercontent.com";

        private readonly RSA _rsa = RSA.Create(2048);

        public GoogleTokenFactory(string keyId = "test-key-1")
        {
            KeyId = keyId;
        }

        public string KeyId { get; }

        public RsaSecurityKey PublicKey => new(_rsa.ExportParameters(false)) { KeyId = KeyId };

        public OpenIdConnectConfiguration CreateConfiguration()
        {
            var configuration = new OpenIdConnectConfiguration { Issuer = Issuer };
            configuration.SigningKeys.Add(PublicKey);
            return configuration;
        }

        public Dictionary<string, object> ValidClaims(string subject, DateTimeOffset now)
        {
            return new Dictionary<string, object>
            {
                ["iss"] = Issuer,
                ["aud"] = ClientId,
                ["sub"] = subject,
                ["iat"] = now.ToUnixTimeSeconds(),
                ["exp"] = now.AddHours(1).ToUnixTimeSeconds()
            };
        }

        public string CreateToken(string subject, DateTimeOffset now)
        {
            return CreateToken(ValidClaims(subject, now));
        }

        public string CreateToken(Dictionary<string, object> claims, string? keyId = null)
        {
            var signingInput = BuildSigningInput(claims, "RS256", keyId ?? KeyId);
            var signature = _rsa.SignData(Encoding.ASCII.GetBytes(signingInput), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            return $"{signingInput}.{Base64UrlEncoder.Encode(signature)}";
        }

        public string CreateUnsignedToken(Dictionary<string, object> claims)
        {
            return $"{BuildSigningInput(claims, "none", KeyId)}.";
        }

        public string CreateTokenSignedWithPublicKeyAsSecret(Dictionary<string, object> claims)
        {
            var signingInput = BuildSigningInput(claims, "HS256", KeyId);
            var secret = _rsa.ExportSubjectPublicKeyInfo();
            var signature = HMACSHA256.HashData(secret, Encoding.ASCII.GetBytes(signingInput));

            return $"{signingInput}.{Base64UrlEncoder.Encode(signature)}";
        }

        private static string BuildSigningInput(Dictionary<string, object> claims, string algorithm, string keyId)
        {
            var header = JsonSerializer.Serialize(new Dictionary<string, object> { ["alg"] = algorithm, ["kid"] = keyId, ["typ"] = "JWT" });
            var payload = JsonSerializer.Serialize(claims);

            return $"{Base64UrlEncoder.Encode(header)}.{Base64UrlEncoder.Encode(payload)}";
        }
    }
}
