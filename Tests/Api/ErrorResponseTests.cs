using System.Net;
using System.Text;
using System.Text.Json;
using MathChain.API.Controllers;
using Microsoft.AspNetCore.Mvc.Testing;

namespace MathChain.Tests.Api
{
    public class ErrorResponseTests : IClassFixture<ErrorResponseTests.ApiFactory>
    {
        private readonly HttpClient _client;

        public ErrorResponseTests(ApiFactory factory)
        {
            _client = factory.CreateClient();
        }

        private static async Task<JsonElement> ReadEnvelopeAsync(HttpResponseMessage response)
        {
            Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            return document.RootElement.Clone();
        }

        [Fact]
        public async Task Post_MalformedJson_ReturnsEnvelopeWithValidationErrors()
        {
            var content = new StringContent("{ this is not json", Encoding.UTF8, "application/json");

            var response = await _client.PostAsync("/api/user/login", content);

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
            var response = await _client.GetAsync("/api/classroom/not-a-guid");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            var envelope = await ReadEnvelopeAsync(response);
            Assert.Equal("Not Found", envelope.GetProperty("message").GetString());
            Assert.Equal(404, envelope.GetProperty("statusCode").GetInt32());
        }

        [Fact]
        public async Task Patch_UnsupportedMethod_ReturnsMethodNotAllowedEnvelope()
        {
            var response = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Patch, "/api/user/login"));

            Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
            var envelope = await ReadEnvelopeAsync(response);
            Assert.Equal("Method Not Allowed", envelope.GetProperty("message").GetString());
            Assert.Equal(405, envelope.GetProperty("statusCode").GetInt32());
        }

        public class ApiFactory : WebApplicationFactory<UserController>
        {
            public ApiFactory()
            {
                Environment.SetEnvironmentVariable("ConnectionStrings__MathchainDB",
                    "Host=localhost;Database=tests;Username=tests;Password=tests");
            }
        }
    }
}
