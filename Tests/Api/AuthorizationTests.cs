using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MathChain.Domain.Entities;
using MathChain.Domain.Enums;

namespace MathChain.Tests.Api
{
    public class AuthorizationTests : IClassFixture<ApiFactory>
    {
        private const string Teacher = "0x1111111111111111111111111111111111111111";
        private const string OtherTeacher = "0x2222222222222222222222222222222222222222";
        private const string Student = "0x3333333333333333333333333333333333333333";

        private readonly ApiFactory _factory;

        public AuthorizationTests(ApiFactory factory)
        {
            _factory = factory;
        }

        private static async Task<JsonElement> ReadEnvelopeAsync(HttpResponseMessage response)
        {
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
            return document.RootElement.Clone();
        }

        private ClassRoom AddClassRoom(string teacherWallet)
        {
            var classRoom = new ClassRoom { Id = Guid.NewGuid(), Name = "Algebra", TeacherWallet = teacherWallet, JoinCode = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant() };
            _factory.ClassRooms.ClassRooms.Add(classRoom);
            return classRoom;
        }

        [Theory]
        [InlineData("/api/classroom/by-teacher/0x1111111111111111111111111111111111111111")]
        [InlineData("/api/dashboard")]
        [InlineData("/api/auth/me")]
        [InlineData("/api/user")]
        public async Task ProtectedEndpoint_WithoutToken_ReturnsUnauthorizedEnvelope(string url)
        {
            var response = await _factory.CreateClient().GetAsync(url, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal(401, (await ReadEnvelopeAsync(response)).GetProperty("statusCode").GetInt32());
        }

        [Fact]
        public async Task UnknownRoute_WithoutToken_ReturnsUnauthorized()
        {
            var response = await _factory.CreateClient().GetAsync("/api/does-not-exist", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task PublicEndpoints_WithoutToken_AreReachable()
        {
            var client = _factory.CreateClient();

            var exists = await client.GetAsync($"/api/user/exists/{Student}", TestContext.Current.CancellationToken);
            var solve = await client.GetAsync("/api/wolfram/solve?query=2%2B3", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, exists.StatusCode);
            Assert.Equal(HttpStatusCode.OK, solve.StatusCode);
        }

        [Fact]
        public async Task CreateClassRoom_AsStudent_ReturnsForbiddenEnvelope()
        {
            var response = await _factory.CreateClientAs(Student, UserRole.Student)
                .PostAsJsonAsync("/api/classroom/create", new { name = "Hacked" }, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Equal("Your role does not allow this action.", (await ReadEnvelopeAsync(response)).GetProperty("message").GetString());
        }

        [Fact]
        public async Task CreateClassRoom_AsTeacher_IgnoresTeacherWalletFromBody()
        {
            var response = await _factory.CreateClientAs(Teacher, UserRole.Teacher)
                .PostAsJsonAsync("/api/classroom/create", new { name = "Geometry", teacherWallet = OtherTeacher }, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.Equal(Teacher, (await ReadEnvelopeAsync(response)).GetProperty("data").GetProperty("teacherWallet").GetString());
        }

        [Fact]
        public async Task DeleteClassRoom_ByAnotherTeacher_ReturnsForbiddenAndKeepsClassRoom()
        {
            var classRoom = AddClassRoom(Teacher);

            var response = await _factory.CreateClientAs(OtherTeacher, UserRole.Teacher)
                .DeleteAsync($"/api/classroom/{classRoom.Id}", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Contains(classRoom, _factory.ClassRooms.ClassRooms);
        }

        [Fact]
        public async Task DeleteClassRoom_ByOwner_ReturnsOk()
        {
            var classRoom = AddClassRoom(Teacher);

            var response = await _factory.CreateClientAs(Teacher, UserRole.Teacher)
                .DeleteAsync($"/api/classroom/{classRoom.Id}", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.DoesNotContain(classRoom, _factory.ClassRooms.ClassRooms);
        }

        [Fact]
        public async Task GetClassRooms_OfAnotherTeacher_ReturnsForbidden()
        {
            var response = await _factory.CreateClientAs(OtherTeacher, UserRole.Teacher)
                .GetAsync($"/api/classroom/by-teacher/{Teacher}", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetAllUsers_AsAdmin_ReturnsOk()
        {
            var response = await _factory.CreateClientAs("0x4444444444444444444444444444444444444444", UserRole.Admin)
                .GetAsync("/api/user", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
