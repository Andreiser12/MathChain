using System.Net;
using System.Text.Json;
using MathChain.Domain.Enums;

namespace MathChain.Tests.Api
{
    public class CurriculumPublicationEndpointTests : IClassFixture<ApiFactory>
    {
        private readonly ApiFactory _factory;

        public CurriculumPublicationEndpointTests(ApiFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task PublishAndVerify_ShouldLetAnyoneReadTheLatestAnchor_WhenAnAdminPublishesTheCurriculum()
        {
            var anonymous = _factory.CreateClient();
            var teacher = _factory.CreateClientAs("0x1111111111111111111111111111111111111113", UserRole.Teacher);
            var admin = _factory.CreateClientAs("0x6666666666666666666666666666666666666668", UserRole.Admin);

            Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/curriculum-publication", TestContext.Current.CancellationToken)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await teacher.PostAsync("/api/curriculum-publication", null, TestContext.Current.CancellationToken)).StatusCode);

            var published = await admin.PostAsync("/api/curriculum-publication", null, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.Created, published.StatusCode);

            using var latest = JsonDocument.Parse(await (await anonymous.GetAsync("/api/curriculum-publication/latest", TestContext.Current.CancellationToken))
                .Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
            var data = latest.RootElement.GetProperty("data");
            var manifest = await anonymous.GetAsync(data.GetProperty("manifestPath").GetString(), TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, manifest.StatusCode);
            Assert.Contains("mathchain.curriculum/v1", await manifest.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
            Assert.Equal("application/json", manifest.Content.Headers.ContentType!.MediaType);
        }
    }
}
