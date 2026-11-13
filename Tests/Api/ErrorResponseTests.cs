using System.Net;
using System.Text;
using System.Text.Json;
using MathChain.Domain.Enums;

namespace MathChain.Tests.Api
{
    public class ErrorResponseTests : IClassFixture<ApiFactory>
    {
        private readonly HttpClient _client;
        private readonly HttpClient _signedInClient;

        public ErrorResponseTests(ApiFactory factory)
        {
            _client = factory.CreateClient();
            _signedInClient = factory.CreateClientAs("0x52908400098527886e0f7030069857d2e4169ee7", UserRole.Student);
        }

        private static async Task<JsonElement> ReadEnvelopeAsync(HttpResponseMessage response)
        {
            Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
            return document.RootElement.Clone();
        }

        [Fact]
        public async Task Post_MalformedJson_ReturnsEnvelopeWithValidationErrors()
        {
            var content = new StringContent("{ this is not json", Encoding.UTF8, "application/json");

            var response = await _client.PostAsync("/api/auth/challenge", content, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var envelope = await ReadEnvelopeAsync(response);
            Assert.False(envelope.GetProperty("success").GetBoolean());
            Assert.Equal("The request is invalid.", envelope.GetProperty("message").GetString());
            Assert.Equal(400, envelope.GetProperty("statusCode").GetInt32());
            Assert.NotEqual(0, envelope.GetProperty("errors").GetArrayLength());
        }

        [Fact]
        public async Task Get_UnknownRoute_ReturnsNotFoundEnvelope()
        {
            var response = await _signedInClient.GetAsync("/api/classroom/not-a-guid", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            var envelope = await ReadEnvelopeAsync(response);
            Assert.Equal("Not Found", envelope.GetProperty("message").GetString());
            Assert.Equal(404, envelope.GetProperty("statusCode").GetInt32());
        }

        [Fact]
        public async Task Patch_UnsupportedMethod_ReturnsMethodNotAllowedEnvelope()
        {
            var response = await _signedInClient.SendAsync(new HttpRequestMessage(HttpMethod.Patch, "/api/auth/challenge"), TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
            var envelope = await ReadEnvelopeAsync(response);
            Assert.Equal("Method Not Allowed", envelope.GetProperty("message").GetString());
            Assert.Equal(405, envelope.GetProperty("statusCode").GetInt32());
        }
    }
}
