using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Nethereum.Signer;

namespace MathChain.Tests.Api
{
    public class AuthEndpointTests : IClassFixture<ApiFactory>
    {
        private readonly HttpClient _client;

        public AuthEndpointTests(ApiFactory factory)
        {
            _client = factory.CreateClient();
        }

        private static async Task<JsonElement> ReadDataAsync(HttpResponseMessage response)
        {
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
            return document.RootElement.GetProperty("data").Clone();
        }

        private async Task<HttpResponseMessage> LoginAsync(EthECKey wallet, string? role)
        {
            var address = wallet.GetPublicAddress();
            var challengeResponse = await _client.PostAsJsonAsync("/api/auth/challenge", new { walletAddress = address }, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, challengeResponse.StatusCode);
            var message = (await ReadDataAsync(challengeResponse)).GetProperty("message").GetString()!;

            var signature = new EthereumMessageSigner().EncodeUTF8AndSign(message, wallet);

            return await _client.PostAsJsonAsync("/api/auth/verify", new { walletAddress = address, signature, role }, TestContext.Current.CancellationToken);
        }

        private async Task<HttpResponseMessage> GetMeAsync(string? token)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
            if (token != null)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            return await _client.SendAsync(request, TestContext.Current.CancellationToken);
        }

        [Fact]
        public async Task SignInFlow_NewStudent_RegistersAndTokenIdentifiesWallet()
        {
            var wallet = EthECKey.GenerateKey();

            var verifyResponse = await LoginAsync(wallet, "Student");

            Assert.Equal(HttpStatusCode.Created, verifyResponse.StatusCode);
            var token = (await ReadDataAsync(verifyResponse)).GetProperty("token").GetString();

            var meResponse = await GetMeAsync(token);

            Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);
            var me = await ReadDataAsync(meResponse);
            Assert.Equal(wallet.GetPublicAddress().ToLowerInvariant(), me.GetProperty("walletAddress").GetString());
            Assert.Equal("Student", me.GetProperty("role").GetString());
        }

        [Fact]
        public async Task SignInFlow_ExistingUser_ReturnsOk()
        {
            var wallet = EthECKey.GenerateKey();
            await LoginAsync(wallet, "Teacher");

            var secondLogin = await LoginAsync(wallet, null);

            Assert.Equal(HttpStatusCode.OK, secondLogin.StatusCode);
        }

        [Fact]
        public async Task Me_WithoutToken_ReturnsUnauthorizedEnvelope()
        {
            var response = await GetMeAsync(null);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
            Assert.Equal(401, document.RootElement.GetProperty("statusCode").GetInt32());
            Assert.Equal("Unauthorized", document.RootElement.GetProperty("message").GetString());
        }

        [Fact]
        public async Task Me_WithTamperedRole_ReturnsUnauthorized()
        {
            var verifyResponse = await LoginAsync(EthECKey.GenerateKey(), "Student");
            var token = (await ReadDataAsync(verifyResponse)).GetProperty("token").GetString()!;
            var parts = token.Split('.');
            var payload = Encoding.UTF8.GetString(Base64UrlDecode(parts[1])).Replace("\"Student\"", "\"Admin\"");
            var tamperedToken = $"{parts[0]}.{Base64UrlEncode(Encoding.UTF8.GetBytes(payload))}.{parts[2]}";

            var response = await GetMeAsync(tamperedToken);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Verify_WithWrongWalletSignature_ReturnsUnauthorizedEnvelope()
        {
            var wallet = EthECKey.GenerateKey();
            var address = wallet.GetPublicAddress();
            await _client.PostAsJsonAsync("/api/auth/challenge", new { walletAddress = address }, TestContext.Current.CancellationToken);
            var forgedSignature = new EthereumMessageSigner().EncodeUTF8AndSign("forged", EthECKey.GenerateKey());

            var response = await _client.PostAsJsonAsync("/api/auth/verify", new { walletAddress = address, signature = forgedSignature, role = "Student" }, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
            Assert.Equal("Invalid wallet signature.", document.RootElement.GetProperty("message").GetString());
        }

        [Fact]
        public async Task OldLoginEndpoint_IsRemoved()
        {
            var response = await _client.PostAsJsonAsync("/api/user/login", new { walletAddress = "0x52908400098527886e0f7030069857d2e4169ee7", role = "Admin" }, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        }

        private static byte[] Base64UrlDecode(string value)
        {
            var base64 = value.Replace('-', '+').Replace('_', '/');
            return Convert.FromBase64String(base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '='));
        }

        private static string Base64UrlEncode(byte[] value)
        {
            return Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }
    }
}
