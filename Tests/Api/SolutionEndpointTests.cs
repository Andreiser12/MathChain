using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MathChain.Domain.Entities;
using MathChain.Domain.Enums;
using MathChain.Tests.Fakes;

namespace MathChain.Tests.Api
{
    public class SolutionEndpointTests : IClassFixture<ApiFactory>
    {
        private const string Payer = "0x8888888888888888888888888888888888888888";

        private readonly ApiFactory _factory;

        public SolutionEndpointTests(ApiFactory factory)
        {
            _factory = factory;
        }

        private static async Task<JsonElement> ReadEnvelopeAsync(HttpResponseMessage response)
        {
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
            return document.RootElement.Clone();
        }

        private CurriculumFormula AddLimitsFormula()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (!File.Exists(Path.Combine(directory!.FullName, "MathChain.sln")))
            {
                directory = directory.Parent;
            }

            var path = Path.Combine(directory.FullName, "MathChain.Blazor", "wwwroot", "data", "curriculum", "calculus_remarcablelimits.json");
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var source = document.RootElement.GetProperty("formulas")[5];
            var formula = new CurriculumFormula
            {
                Id = Guid.NewGuid(),
                ChapterId = Guid.NewGuid(),
                FormulaCode = source.GetProperty("id").GetString()!,
                Title = "sin x / x",
                ExercisesJson = source.GetProperty("exercises").GetRawText()
            };
            _factory.Curriculum.Formulas.Add(formula);
            return formula;
        }

        [Fact]
        public async Task SolutionPurchase_ShouldShowTheSolutionWithTheNumbersOfTheExercise_WhenTheStudentPays()
        {
            var formula = AddLimitsFormula();
            var studentWallet = "0x5555555555555555555555555555555555555561";
            var student = _factory.CreateClientAs(studentWallet, UserRole.Student);
            var token = TestContext.Current.CancellationToken;

            var offer = (await ReadEnvelopeAsync(await student.GetAsync($"/api/solutions/formulas/{formula.Id}", token))).GetProperty("data");
            Assert.True(offer.GetProperty("canPurchase").GetBoolean());
            var items = offer.GetProperty("exercises").EnumerateArray().ToList();
            Assert.Equal(9, items.Count);
            Assert.All(items, item => Assert.True(item.GetProperty("hasSolution").GetBoolean()));
            Assert.Equal(new[] { 1m, 1m, 1m, 2m, 2m, 2m, 3m, 3m, 3m }, items.Select(i => i.GetProperty("price").GetDecimal()));

            var exercise = (await ReadEnvelopeAsync(await student.PostAsync($"/api/practice/formulas/{formula.Id}/exercises/0", null, token))).GetProperty("data");
            var exerciseId = exercise.GetProperty("exerciseId").GetGuid();
            var notPaid = await student.GetAsync($"/api/solutions/exercises/{exerciseId}", token);
            Assert.Equal(HttpStatusCode.Forbidden, notPaid.StatusCode);

            var quote = (await ReadEnvelopeAsync(await student.PostAsJsonAsync(
                $"/api/solutions/formulas/{formula.Id}/exercises/0/quote", new { payerAddress = Payer }, token))).GetProperty("data");
            Assert.Equal(1m, quote.GetProperty("price").GetDecimal());
            using (var typedData = JsonDocument.Parse(quote.GetProperty("typedData").GetString()!))
            {
                Assert.Equal("1000000", typedData.RootElement.GetProperty("message").GetProperty("value").GetString());
            }

            var purchaseId = quote.GetProperty("purchaseId").GetGuid();
            var payment = (await ReadEnvelopeAsync(await student.PostAsJsonAsync(
                $"/api/solutions/purchases/{purchaseId}", new { signature = FakeSolutionPaymentRegistry.ValidSignature }, token))).GetProperty("data");
            Assert.Equal("Completed", payment.GetProperty("status").GetString());

            var solution = (await ReadEnvelopeAsync(await student.GetAsync($"/api/solutions/exercises/{exerciseId}", token))).GetProperty("data");
            var a = _factory.Practice.Exercises.Single(e => e.Id == exerciseId).VariableValues!;
            var value = JsonDocument.Parse(a).RootElement.GetProperty("a").GetInt32();
            var steps = solution.GetProperty("steps").EnumerateArray().ToList();
            Assert.Equal($"Multiply and divide by {value}.", steps[0].GetProperty("text").GetString());
            Assert.EndsWith($"= {value}", steps[^1].GetProperty("latex").GetString());
            Assert.Equal(value.ToString(), solution.GetProperty("answer").GetString());

            var otherStudent = _factory.CreateClientAs("0x5555555555555555555555555555555555555562", UserRole.Student);
            Assert.Equal(HttpStatusCode.NotFound, (await otherStudent.GetAsync($"/api/solutions/exercises/{exerciseId}", token)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await otherStudent.GetAsync($"/api/solutions/purchases/{purchaseId}", token)).StatusCode);
        }

        [Fact]
        public async Task SubmitPayment_ShouldReturnBadRequest_WhenTheSignatureIsMalformed()
        {
            var formula = AddLimitsFormula();
            var student = _factory.CreateClientAs("0x5555555555555555555555555555555555555563", UserRole.Student);
            var token = TestContext.Current.CancellationToken;
            var quote = (await ReadEnvelopeAsync(await student.PostAsJsonAsync(
                $"/api/solutions/formulas/{formula.Id}/exercises/8/quote", new { payerAddress = Payer }, token))).GetProperty("data");

            var response = await student.PostAsJsonAsync($"/api/solutions/purchases/{quote.GetProperty("purchaseId").GetGuid()}",
                new { signature = "0xbad" }, token);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("The signature does not match the paying wallet. Sign the payment again.",
                (await ReadEnvelopeAsync(response)).GetProperty("message").GetString());
        }

        [Fact]
        public async Task SolutionEndpoints_ShouldRequireLoginAndKeepTheSetupForAdmins_WhenCalledByOtherUsers()
        {
            var token = TestContext.Current.CancellationToken;
            var anonymous = _factory.CreateClient();

            Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync($"/api/solutions/formulas/{Guid.NewGuid()}", token)).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/solutions/setup", token)).StatusCode);

            var teacher = _factory.CreateClientAs("0x5555555555555555555555555555555555555564", UserRole.Teacher);
            Assert.Equal(HttpStatusCode.Forbidden, (await teacher.GetAsync("/api/solutions/setup", token)).StatusCode);

            var admin = _factory.CreateClientAs("0x5555555555555555555555555555555555555565", UserRole.Admin);
            var setup = (await ReadEnvelopeAsync(await admin.GetAsync("/api/solutions/setup", token))).GetProperty("data");
            Assert.Equal(3m, setup.GetProperty("hardPrice").GetDecimal());

            var change = (await ReadEnvelopeAsync(await admin.PostAsJsonAsync("/api/solutions/setup/prices",
                new { difficulty = "Hard", price = 4.5m }, token))).GetProperty("data");
            Assert.Equal("setPrice:Hard:4500000", change.GetProperty("data").GetString());
            Assert.Equal(FakeSolutionPaymentRegistry.ContractAddress, change.GetProperty("to").GetString());
        }
    }
}
