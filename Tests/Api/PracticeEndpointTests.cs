using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MathChain.Domain.Entities;
using MathChain.Domain.Enums;

namespace MathChain.Tests.Api
{
    public class PracticeEndpointTests : IClassFixture<ApiFactory>
    {
        private readonly ApiFactory _factory;

        public PracticeEndpointTests(ApiFactory factory)
        {
            _factory = factory;
        }

        private static async Task<JsonElement> ReadEnvelopeAsync(HttpResponseMessage response)
        {
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
            return document.RootElement.Clone();
        }

        private (CurriculumChapter Chapter, CurriculumFormula Formula) AddChapterWithOneExercise()
        {
            var chapter = new CurriculumChapter { Id = Guid.NewGuid(), Title = "Calcul prescurtat", ChapterSlug = $"chapter_{Guid.NewGuid():N}", Version = 1 };
            var formula = new CurriculumFormula
            {
                Id = Guid.NewGuid(),
                ChapterId = chapter.Id,
                Title = "Patratul sumei",
                ExercisesJson = JsonSerializer.Serialize(new[] { new { difficulty = "Easy", wolframQuery = "expand (x + {a})^2", latexTemplate = "(x + {a})^2", variables = new { a = new[] { 4, 4 } } } })
            };
            _factory.Curriculum.Chapters.Add(chapter);
            _factory.Curriculum.Formulas.Add(formula);
            return (chapter, formula);
        }

        [Fact]
        public async Task PracticeThenClaim_StudentSolvesTheChapterAndReceivesACertificate()
        {
            var (chapter, formula) = AddChapterWithOneExercise();
            var student = _factory.CreateClientAs("0x5555555555555555555555555555555555555551", UserRole.Student);

            var exercise = (await ReadEnvelopeAsync(await student.PostAsync($"/api/practice/formulas/{formula.Id}/exercises/0", null, TestContext.Current.CancellationToken))).GetProperty("data");
            Assert.Equal("(x + 4)^2", exercise.GetProperty("latex").GetString());
            Assert.Equal("Expanded", exercise.GetProperty("form").GetString());
            Assert.False(exercise.TryGetProperty("expectedAnswer", out _));

            var tooEarly = await student.PostAsync($"/api/certificates/chapters/{chapter.Id}", null, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.BadRequest, tooEarly.StatusCode);

            var exerciseId = exercise.GetProperty("exerciseId").GetGuid();
            var wrong = (await ReadEnvelopeAsync(await student.PostAsJsonAsync($"/api/practice/exercises/{exerciseId}/answer", new { answer = "(x + 4)^2" }, TestContext.Current.CancellationToken))).GetProperty("data");
            Assert.Equal("WrongForm", wrong.GetProperty("verdict").GetString());
            var right = (await ReadEnvelopeAsync(await student.PostAsJsonAsync($"/api/practice/exercises/{exerciseId}/answer", new { answer = "x^2 + 8x + 16" }, TestContext.Current.CancellationToken))).GetProperty("data");
            Assert.True(right.GetProperty("correct").GetBoolean());

            var progress = (await ReadEnvelopeAsync(await student.GetAsync($"/api/certificates/chapters/{chapter.Id}/progress", TestContext.Current.CancellationToken))).GetProperty("data");
            Assert.True(progress.GetProperty("completed").GetBoolean());

            var claim = await student.PostAsync($"/api/certificates/chapters/{chapter.Id}", null, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.Created, claim.StatusCode);
            var certificate = (await ReadEnvelopeAsync(claim)).GetProperty("data");
            Assert.Equal("Chapter", certificate.GetProperty("kind").GetString());

            var verification = (await ReadEnvelopeAsync(await _factory.CreateClient().GetAsync(
                $"/api/certificates/verify/{certificate.GetProperty("tokenId").GetInt64()}?key={certificate.GetProperty("verificationKey").GetString()}",
                TestContext.Current.CancellationToken))).GetProperty("data");
            Assert.True(verification.GetProperty("isValid").GetBoolean());
            Assert.Equal("Calcul prescurtat", verification.GetProperty("subjectTitle").GetString());
        }

        [Fact]
        public async Task PracticeEndpoints_RequireLogin()
        {
            var (chapter, formula) = AddChapterWithOneExercise();
            var anonymous = _factory.CreateClient();

            Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync($"/api/practice/formulas/{formula.Id}", TestContext.Current.CancellationToken)).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsync($"/api/practice/formulas/{formula.Id}/exercises/0", null, TestContext.Current.CancellationToken)).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync($"/api/certificates/chapters/{chapter.Id}/progress", TestContext.Current.CancellationToken)).StatusCode);
        }

        [Fact]
        public async Task AnswerToAnotherStudentsExercise_ReturnsNotFound()
        {
            var (_, formula) = AddChapterWithOneExercise();
            var owner = _factory.CreateClientAs("0x5555555555555555555555555555555555555552", UserRole.Student);
            var intruder = _factory.CreateClientAs("0x5555555555555555555555555555555555555553", UserRole.Student);
            var exercise = (await ReadEnvelopeAsync(await owner.PostAsync($"/api/practice/formulas/{formula.Id}/exercises/0", null, TestContext.Current.CancellationToken))).GetProperty("data");

            var response = await intruder.PostAsJsonAsync($"/api/practice/exercises/{exercise.GetProperty("exerciseId").GetGuid()}/answer", new { answer = "x^2 + 8x + 16" }, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task TeacherCannotClaimAChapterCertificate()
        {
            var (chapter, _) = AddChapterWithOneExercise();

            var response = await _factory.CreateClientAs("0x5555555555555555555555555555555555555554", UserRole.Teacher)
                .PostAsync($"/api/certificates/chapters/{chapter.Id}", null, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Theory]
        [InlineData("POST", "/api/dashboard/record-solved")]
        [InlineData("GET", "/api/dashboard/practice-progress?formulaId=x")]
        public async Task OldSelfReportedPracticeEndpoints_AreRemoved(string method, string url)
        {
            var client = _factory.CreateClientAs("0x5555555555555555555555555555555555555555", UserRole.Student);

            var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), url)
            {
                Content = method == "POST" ? JsonContent.Create(new { formulaId = "x", exerciseIndex = 0 }) : null
            }, TestContext.Current.CancellationToken);

            Assert.True(response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.MethodNotAllowed, response.StatusCode.ToString());
        }
    }
}
