using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using MathChain.Tests.Shared.Fakes;

namespace MathChain.API.IntegrationTests
{
    public class GoogleSignInEndpointTests : IClassFixture<ApiFactory>
    {
        private readonly ApiFactory _factory;
        private readonly HttpClient _client;

        public GoogleSignInEndpointTests(ApiFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        private static async Task<JsonElement> ReadEnvelopeAsync(HttpResponseMessage response)
        {
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
            return document.RootElement.Clone();
        }

        private string NewIdToken(string subject)
        {
            return _factory.Google.CreateToken(subject, DateTimeOffset.UtcNow);
        }

        private Task<HttpResponseMessage> SignInAsync(string idToken, string? role)
        {
            return _client.PostAsJsonAsync("/api/auth/google", new { idToken, role }, TestContext.Current.CancellationToken);
        }

        [Fact]
        public async Task Providers_ShouldReturnTheGoogleClientId_WhenNoTokenIsSent()
        {
            var response = await _client.GetAsync("/api/auth/providers", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(GoogleTokenFactory.ClientId, (await ReadEnvelopeAsync(response)).GetProperty("data").GetProperty("googleClientId").GetString());
        }

        [Fact]
        public async Task GoogleSignIn_ShouldRegisterTheUserAndAllowProtectedEndpoints_WhenTheTeacherIsNew()
        {
            var subject = Guid.NewGuid().ToString("N");

            var withoutRole = await SignInAsync(NewIdToken(subject), null);
            Assert.Equal(HttpStatusCode.NotFound, withoutRole.StatusCode);
            Assert.Equal(404, (await ReadEnvelopeAsync(withoutRole)).GetProperty("statusCode").GetInt32());

            var registered = await SignInAsync(NewIdToken(subject), "Teacher");
            Assert.Equal(HttpStatusCode.Created, registered.StatusCode);
            var data = (await ReadEnvelopeAsync(registered)).GetProperty("data");
            var address = data.GetProperty("user").GetProperty("walletAddress").GetString()!;
            Assert.Matches("^0x[0-9a-f]{40}$", address);

            var authorized = _factory.CreateClient();
            authorized.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", data.GetProperty("token").GetString());

            var me = (await ReadEnvelopeAsync(await authorized.GetAsync("/api/auth/me", TestContext.Current.CancellationToken))).GetProperty("data");
            Assert.Equal(address, me.GetProperty("walletAddress").GetString());
            Assert.Equal("Teacher", me.GetProperty("role").GetString());

            var classRoom = await authorized.PostAsJsonAsync("/api/classroom/create", new { name = "Algebra" }, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.Created, classRoom.StatusCode);
            Assert.Equal(address, (await ReadEnvelopeAsync(classRoom)).GetProperty("data").GetProperty("teacherWallet").GetString());
        }

        [Fact]
        public async Task GoogleSignIn_ShouldReturnOkWithTheSameAddress_WhenTheUserReturns()
        {
            var subject = Guid.NewGuid().ToString("N");
            var first = (await ReadEnvelopeAsync(await SignInAsync(NewIdToken(subject), "Student"))).GetProperty("data");

            var secondResponse = await SignInAsync(NewIdToken(subject), null);

            Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
            var second = (await ReadEnvelopeAsync(secondResponse)).GetProperty("data");
            Assert.Equal(first.GetProperty("user").GetProperty("walletAddress").GetString(), second.GetProperty("user").GetProperty("walletAddress").GetString());
            Assert.False(second.GetProperty("isNewUser").GetBoolean());
        }

        [Fact]
        public async Task GoogleSignIn_ShouldReturnAnUnauthorizedEnvelope_WhenTheTokenIsSignedByAnotherKey()
        {
            var forged = new GoogleTokenFactory().CreateToken("attacker", DateTimeOffset.UtcNow);

            var response = await SignInAsync(forged, "Teacher");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal("Invalid Google sign-in token.", (await ReadEnvelopeAsync(response)).GetProperty("message").GetString());
        }

        [Fact]
        public async Task GoogleSignIn_ShouldReturnBadRequest_WhenTheRequestedRoleIsAdmin()
        {
            var response = await SignInAsync(NewIdToken(Guid.NewGuid().ToString("N")), "Admin");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GoogleSignIn_ShouldReturnABadRequestEnvelope_WhenTheTokenIsMissing()
        {
            var response = await _client.PostAsJsonAsync("/api/auth/google", new { role = "Student" }, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("Google ID token is required.", (await ReadEnvelopeAsync(response)).GetProperty("message").GetString());
        }
    }
}
