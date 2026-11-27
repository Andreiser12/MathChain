using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MathChain.Domain.Entities;
using MathChain.Domain.Enums;

namespace MathChain.Tests.Api
{
    public class ChapterPrerequisiteEndpointTests : IClassFixture<ApiFactory>
    {
        private readonly ApiFactory _factory;

        public ChapterPrerequisiteEndpointTests(ApiFactory factory)
        {
            _factory = factory;
        }

        private static async Task<JsonElement> ReadEnvelopeAsync(HttpResponseMessage response)
        {
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
            return document.RootElement.Clone();
        }

        private (CurriculumChapter Root, CurriculumChapter First, CurriculumChapter Second, CurriculumFormula SecondFormula) AddTree()
        {
            var suffix = Guid.NewGuid().ToString("N");
            var root = new CurriculumChapter { Id = Guid.NewGuid(), Title = "Algebra", ChapterSlug = $"root_{suffix}", Version = 1 };
            var first = new CurriculumChapter { Id = Guid.NewGuid(), ParentId = root.Id, Title = "Powers", ChapterSlug = $"first_{suffix}", Order = 1, Version = 1 };
            var second = new CurriculumChapter { Id = Guid.NewGuid(), ParentId = root.Id, Title = "Radicals", ChapterSlug = $"second_{suffix}", Order = 2, Version = 1 };
            var exercises = JsonSerializer.Serialize(new[] { new { difficulty = "Easy", wolframQuery = "expand (x + {a})^2", latexTemplate = "(x + {a})^2", variables = new { a = new[] { 4, 4 } } } });
            var secondFormula = new CurriculumFormula { Id = Guid.NewGuid(), ChapterId = second.Id, Title = "Radical", ExercisesJson = exercises };

            _factory.Curriculum.Chapters.AddRange(new[] { root, first, second });
            _factory.Curriculum.Formulas.Add(new CurriculumFormula { Id = Guid.NewGuid(), ChapterId = first.Id, Title = "Power", ExercisesJson = exercises });
            _factory.Curriculum.Formulas.Add(secondFormula);

            return (root, first, second, secondFormula);
        }

        [Fact]
        public async Task SetPrerequisites_ShouldAcceptTheChangeOnly_WhenTheUserIsAnAdmin()
        {
            var (_, first, second, _) = AddTree();
            var body = new { prerequisiteIds = new[] { first.Id } };

            var anonymous = await _factory.CreateClient().PutAsJsonAsync($"/api/curriculum/{second.Id}/prerequisites", body, TestContext.Current.CancellationToken);
            var teacher = await _factory.CreateClientAs("0x1111111111111111111111111111111111111112", UserRole.Teacher)
                .PutAsJsonAsync($"/api/curriculum/{second.Id}/prerequisites", body, TestContext.Current.CancellationToken);
            var admin = await _factory.CreateClientAs("0x6666666666666666666666666666666666666667", UserRole.Admin)
                .PutAsJsonAsync($"/api/curriculum/{second.Id}/prerequisites", body, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, teacher.StatusCode);
            Assert.Equal(HttpStatusCode.OK, admin.StatusCode);
            Assert.Equal(first.Id, (await ReadEnvelopeAsync(admin)).GetProperty("data").GetProperty("prerequisiteIds")[0].GetGuid());
        }

        [Fact]
        public async Task SetPrerequisites_ShouldReturn400WithTheCycle_WhenTheChangeCreatesACycle()
        {
            var (_, first, second, _) = AddTree();
            var admin = _factory.CreateClientAs("0x6666666666666666666666666666666666666667", UserRole.Admin);
            await admin.PutAsJsonAsync($"/api/curriculum/{second.Id}/prerequisites", new { prerequisiteIds = new[] { first.Id } }, TestContext.Current.CancellationToken);

            var cycle = await admin.PutAsJsonAsync($"/api/curriculum/{first.Id}/prerequisites", new { prerequisiteIds = new[] { second.Id } }, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, cycle.StatusCode);
            Assert.Contains("cycle", (await ReadEnvelopeAsync(cycle)).GetProperty("message").GetString());
        }

        [Fact]
        public async Task LockedChapter_ShouldShowTheLockAndBlockPractice_WhenTheStudentHasNotFinishedThePrerequisite()
        {
            var (root, first, second, secondFormula) = AddTree();
            await _factory.CreateClientAs("0x6666666666666666666666666666666666666667", UserRole.Admin)
                .PutAsJsonAsync($"/api/curriculum/{second.Id}/prerequisites", new { prerequisiteIds = new[] { first.Id } }, TestContext.Current.CancellationToken);
            var student = _factory.CreateClientAs("0x5555555555555555555555555555555555555561", UserRole.Student);

            var detail = (await ReadEnvelopeAsync(await student.GetAsync($"/api/curriculum/{root.ChapterSlug}", TestContext.Current.CancellationToken))).GetProperty("data");
            var practice = await student.PostAsync($"/api/practice/formulas/{secondFormula.Id}/exercises/0", null, TestContext.Current.CancellationToken);
            var visitor = (await ReadEnvelopeAsync(await _factory.CreateClient().GetAsync($"/api/curriculum/{root.ChapterSlug}", TestContext.Current.CancellationToken))).GetProperty("data");

            var locked = detail.GetProperty("subChapters").EnumerateArray().Single(c => c.GetProperty("id").GetGuid() == second.Id);
            Assert.True(locked.GetProperty("locked").GetBoolean());
            Assert.Equal("Powers", locked.GetProperty("missingPrerequisites")[0].GetString());
            Assert.Equal(HttpStatusCode.Forbidden, practice.StatusCode);
            Assert.Equal("This chapter is locked. Finish Powers first.", (await ReadEnvelopeAsync(practice)).GetProperty("message").GetString());
            Assert.All(visitor.GetProperty("subChapters").EnumerateArray(), c => Assert.False(c.GetProperty("locked").GetBoolean()));
        }
    }
}
