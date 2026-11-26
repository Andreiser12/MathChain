using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MathChain.Domain.Enums;

namespace MathChain.Tests.Api
{
    public class IpfsEndpointTests : IClassFixture<ApiFactory>
    {
        private readonly ApiFactory _factory;

        public IpfsEndpointTests(ApiFactory factory)
        {
            _factory = factory;
        }

        private static MultipartFormDataContent FileForm(byte[] content, string name)
        {
            var form = new MultipartFormDataContent();
            var file = new ByteArrayContent(content);
            file.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            form.Add(file, "file", name);
            return form;
        }

        private async Task<string> UploadAsync(byte[] content)
        {
            var student = _factory.CreateClientAs("0x5555555555555555555555555555555555555571", UserRole.Student);
            var response = await student.PostAsync("/api/ipfs/upload", FileForm(content, "notes.txt"), TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
            return document.RootElement.GetProperty("data").GetString()!;
        }

        [Fact]
        public async Task UploadThenDownload_ReturnsTheSameBytesWithImmutableCaching()
        {
            var content = Encoding.UTF8.GetBytes("homework " + Guid.NewGuid());
            var cid = await UploadAsync(content);

            var response = await _factory.CreateClient().GetAsync($"/api/ipfs/{cid}?fileName=notes.txt", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.StartsWith("bafkrei", cid);
            Assert.Equal(content, await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken));
            Assert.Equal("text/plain", response.Content.Headers.ContentType!.MediaType);
            Assert.Equal($"\"{cid}\"", response.Headers.ETag!.Tag);
            Assert.Contains("immutable", response.Headers.CacheControl!.ToString());
            Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
            Assert.Equal("notes.txt", response.Content.Headers.ContentDisposition!.FileName?.Trim('"'));
        }

        [Fact]
        public async Task Download_MatchingETag_Returns304()
        {
            var cid = await UploadAsync(Encoding.UTF8.GetBytes("cached " + Guid.NewGuid()));
            var request = new HttpRequestMessage(HttpMethod.Get, $"/api/ipfs/{cid}");
            request.Headers.IfNoneMatch.Add(new EntityTagHeaderValue($"\"{cid}\""));

            var response = await _factory.CreateClient().SendAsync(request, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.NotModified, response.StatusCode);
        }

        [Theory]
        [InlineData("..%5C..%5Cappsettings.json")]
        [InlineData("..%5Cappsettings.Development.json")]
        [InlineData("%2E%2E%5C%2E%2E%5Cappsettings.json")]
        [InlineData("Qm")]
        [InlineData("QmYTNyRmU9Ms8voG3bSKm9xVvVH4om65q7FuiYuDLGDpMs.png")]
        [InlineData("not-a-cid")]
        public async Task Download_InvalidIdentifier_Returns400(string value)
        {
            var response = await _factory.CreateClient().GetAsync($"/api/ipfs/{value}", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.DoesNotContain("ConnectionStrings", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task Download_UnknownIdentifier_Returns404()
        {
            var response = await _factory.CreateClient().GetAsync("/api/ipfs/bafkreihdwdcefgh4dqkjv67uzcmw7ojee6xedzdetojuzjevtenxquvyku", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Download_FileNameWithPath_KeepsOnlyTheName()
        {
            var cid = await UploadAsync(Encoding.UTF8.GetBytes("named " + Guid.NewGuid()));

            var response = await _factory.CreateClient().GetAsync($"/api/ipfs/{cid}?fileName=..%5C..%5Creport.pdf", TestContext.Current.CancellationToken);

            Assert.Equal("report.pdf", response.Content.Headers.ContentDisposition!.FileName?.Trim('"'));
            Assert.Equal("application/pdf", response.Content.Headers.ContentType!.MediaType);
        }

        [Fact]
        public async Task Upload_LargerThanTenMegabytes_Returns400()
        {
            var student = _factory.CreateClientAs("0x5555555555555555555555555555555555555572", UserRole.Student);

            var response = await student.PostAsync("/api/ipfs/upload", FileForm(new byte[10 * 1024 * 1024 + 1], "big.bin"), TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains("10 MB", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task Upload_Anonymous_Returns401()
        {
            var response = await _factory.CreateClient().PostAsync("/api/ipfs/upload", FileForm(new byte[] { 1 }, "a.bin"), TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
