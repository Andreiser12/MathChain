using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MathChain.Domain.Entities;
using MathChain.Domain.Enums;
using MathChain.Tests.Shared.Fakes;

namespace MathChain.API.IntegrationTests
{
    public class CertificateEndpointTests : IClassFixture<ApiFactory>
    {
        private const string Teacher = "0x1111111111111111111111111111111111111111";
        private const string Student = "0x3333333333333333333333333333333333333333";

        private readonly ApiFactory _factory;

        public CertificateEndpointTests(ApiFactory factory)
        {
            _factory = factory;
        }

        private static async Task<JsonElement> ReadEnvelopeAsync(HttpResponseMessage response)
        {
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
            return document.RootElement.Clone();
        }

        private ClassRoom AddClassRoomWithGradedStudent(string studentWallet)
        {
            var classRoom = new ClassRoom { Id = Guid.NewGuid(), Name = "Geometry", TeacherWallet = Teacher, JoinCode = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant() };
            _factory.ClassRooms.ClassRooms.Add(classRoom);
            _factory.ClassRooms.Enrollments.Add(new Enrollment { Id = Guid.NewGuid(), ClassRoomId = classRoom.Id, StudentWallet = studentWallet, FinalGrade = 10 });
            return classRoom;
        }

        [Fact]
        public async Task IssueThenVerify_ShouldShowTheGradeToAnyoneWithTheKey_WhenTheTeacherIssuesTheCertificate()
        {
            var classRoom = AddClassRoomWithGradedStudent(Student);
            var teacher = _factory.CreateClientAs(Teacher, UserRole.Teacher);
            var student = _factory.CreateClientAs(Student, UserRole.Student);
            var anonymous = _factory.CreateClient();

            var issueResponse = await teacher.PostAsJsonAsync($"/api/classroom/{classRoom.Id}/certificates", new { }, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.Created, issueResponse.StatusCode);
            var issued = (await ReadEnvelopeAsync(issueResponse)).GetProperty("data")[0];
            Assert.Equal("Issued", issued.GetProperty("status").GetString());
            Assert.False(issued.TryGetProperty("salt", out _));
            Assert.False(issued.TryGetProperty("verificationKey", out _));
            var tokenId = issued.GetProperty("tokenId").GetInt64();

            var mine = (await ReadEnvelopeAsync(await student.GetAsync("/api/certificates/mine", TestContext.Current.CancellationToken))).GetProperty("data");
            var key = mine.EnumerateArray().Single(c => c.GetProperty("tokenId").GetInt64() == tokenId).GetProperty("verificationKey").GetString();

            var publicView = (await ReadEnvelopeAsync(await anonymous.GetAsync($"/api/certificates/verify/{tokenId}", TestContext.Current.CancellationToken))).GetProperty("data");
            Assert.True(publicView.GetProperty("isValid").GetBoolean());
            Assert.False(publicView.GetProperty("detailsRevealed").GetBoolean());
            Assert.Equal(JsonValueKind.Null, publicView.GetProperty("grade").ValueKind);

            var withKey = (await ReadEnvelopeAsync(await anonymous.GetAsync($"/api/certificates/verify/{tokenId}?key={key}", TestContext.Current.CancellationToken))).GetProperty("data");
            Assert.True(withKey.GetProperty("dataMatchesBlockchain").GetBoolean());
            Assert.Equal(10, withKey.GetProperty("grade").GetInt32());
            Assert.Equal("Geometry", withKey.GetProperty("subjectTitle").GetString());
            Assert.Equal("Class", withKey.GetProperty("kind").GetString());
        }

        [Fact]
        public async Task Verify_ShouldReturnAnErrorEnvelope_WhenTheTokenIsUnknownOrTheKeyIsWrong()
        {
            var classRoom = AddClassRoomWithGradedStudent("0x7777777777777777777777777777777777777777");
            var issueResponse = await _factory.CreateClientAs(Teacher, UserRole.Teacher)
                .PostAsJsonAsync($"/api/classroom/{classRoom.Id}/certificates", new { }, TestContext.Current.CancellationToken);
            var tokenId = (await ReadEnvelopeAsync(issueResponse)).GetProperty("data")[0].GetProperty("tokenId").GetInt64();
            var anonymous = _factory.CreateClient();

            var unknown = await anonymous.GetAsync("/api/certificates/verify/999999", TestContext.Current.CancellationToken);
            var wrongKey = await anonymous.GetAsync($"/api/certificates/verify/{tokenId}?key=0x{new string('0', 64)}", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, wrongKey.StatusCode);
            Assert.Equal("The verification key does not match this certificate.", (await ReadEnvelopeAsync(wrongKey)).GetProperty("message").GetString());
        }

        [Fact]
        public async Task ProtectedCertificateEndpoints_ShouldRejectTheRequest_WhenTheUserIsAnonymousAStudentOrNotAnAdmin()
        {
            var classRoom = AddClassRoomWithGradedStudent("0x8888888888888888888888888888888888888888");
            var anonymous = _factory.CreateClient();
            var student = _factory.CreateClientAs("0x8888888888888888888888888888888888888888", UserRole.Student);
            var teacher = _factory.CreateClientAs(Teacher, UserRole.Teacher);

            Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync($"/api/classroom/{classRoom.Id}/certificates", new { }, TestContext.Current.CancellationToken)).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/certificates/mine", TestContext.Current.CancellationToken)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await student.PostAsJsonAsync($"/api/classroom/{classRoom.Id}/certificates", new { }, TestContext.Current.CancellationToken)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await student.GetAsync($"/api/classroom/{classRoom.Id}/certificates", TestContext.Current.CancellationToken)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await teacher.GetAsync("/api/certificates/setup", TestContext.Current.CancellationToken)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await student.GetAsync($"/api/certificates/balance/{Teacher}", TestContext.Current.CancellationToken)).StatusCode);
        }

        [Fact]
        public async Task Setup_ShouldReturnTheIssuerAndTheNetwork_WhenTheUserIsAnAdmin()
        {
            var admin = _factory.CreateClientAs("0x6666666666666666666666666666666666666666", UserRole.Admin);

            var setup = (await ReadEnvelopeAsync(await admin.GetAsync("/api/certificates/setup", TestContext.Current.CancellationToken))).GetProperty("data");

            Assert.Equal(FakeCertificateRegistry.IssuerAddress, setup.GetProperty("issuerAddress").GetString());
            Assert.Equal(31337, setup.GetProperty("chainId").GetInt64());
            Assert.StartsWith("0x2f2ff15d", setup.GetProperty("grantIssuerRoleCallData").GetString());
        }

        [Fact]
        public async Task OldIntegralEndpoints_ShouldReturnNotFound_WhenTheyAreCalled()
        {
            var response = await _factory.CreateClientAs(Teacher, UserRole.Teacher)
                .GetAsync($"/api/integral/balance/{Teacher}", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
