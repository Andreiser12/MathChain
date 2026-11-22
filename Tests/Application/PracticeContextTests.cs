using System.Text.Json;
using MathChain.Application.Contexts;
using MathChain.Application.DTOs;
using MathChain.Application.Practice;
using MathChain.Domain.Entities;
using MathChain.Domain.Enums;
using MathChain.Domain.Exceptions;
using MathChain.Tests.Fakes;

namespace MathChain.Tests.Application
{
    public class PracticeContextTests
    {
        private const string Student = "0x3333333333333333333333333333333333333333";
        private const string OtherStudent = "0x4444444444444444444444444444444444444444";

        private readonly InMemoryCurriculumGateway _curriculum = new();
        private readonly InMemoryPracticeExerciseGateway _practice = new();
        private readonly FakeCurrentUser _currentUser = new();
        private readonly ManualTimeProvider _time = new();
        private readonly PracticeContext _context;
        private readonly CurriculumFormula _formula;

        public PracticeContextTests()
        {
            _context = new PracticeContext(_curriculum, _practice, new AccessGuard(_currentUser, new InMemoryClassRoomGateway()), _time,
                new PracticeRandom(new Random(7)));

            _formula = new CurriculumFormula
            {
                Id = Guid.NewGuid(),
                ChapterId = Guid.NewGuid(),
                Title = "Patratul sumei",
                ExercisesJson = JsonSerializer.Serialize(new object[]
                {
                    new { difficulty = "Easy", wolframQuery = "expand (x + {a})^2", latexTemplate = "(x + {a})^2", variables = new { a = new[] { 2, 2 } } },
                    new { difficulty = "Hard", wolframQuery = "factor x^2 - {a*a}", latexTemplate = "x^2 - {a*a}", variables = new { a = new[] { 3, 3 } } }
                })
            };
            _curriculum.Formulas.Add(_formula);
            _currentUser.As(Student, UserRole.Student);
        }

        private Task<PracticeAnswerResultDto> AnswerAsync(Guid exerciseId, string answer)
        {
            return _context.SubmitAnswerAsync(exerciseId, new SubmitPracticeAnswerRequest { Answer = answer });
        }

        [Fact]
        public async Task GetFormulaAsync_ListsTheFormulaOwnExercises()
        {
            var formula = await _context.GetFormulaAsync(_formula.Id);

            Assert.False(formula.UsesGenericExercises);
            Assert.Equal("Patratul sumei", formula.Title);
            Assert.Equal(new[] { AnswerForm.Expanded, AnswerForm.Factored }, formula.Exercises.Select(e => e.Form));
            Assert.Empty(formula.SolvedIndices);
        }

        [Fact]
        public async Task GetFormulaAsync_UnknownFormula_UsesTheGenericExercises()
        {
            var formula = await _context.GetFormulaAsync(Guid.NewGuid());

            Assert.True(formula.UsesGenericExercises);
            Assert.Equal(9, formula.Exercises.Count);
        }

        [Fact]
        public async Task StartExerciseAsync_CreatesAnExerciseWithoutRevealingTheAnswer()
        {
            var exercise = await _context.StartExerciseAsync(_formula.Id, 0);

            Assert.Equal("(x + 2)^2", exercise.Latex);
            Assert.Equal(AnswerForm.Expanded, exercise.Form);
            Assert.Equal(PracticeContext.MaximumAttempts, exercise.AttemptsLeft);
            Assert.Equal(_time.Now.UtcDateTime + PracticeContext.ExerciseLifetime, exercise.ExpiresAt);
            Assert.DoesNotContain(typeof(PracticeExerciseDto).GetProperties(), p => p.Name.Contains("Answer"));
            var stored = Assert.Single(_practice.Exercises);
            Assert.Equal(Student, stored.StudentWallet);
            Assert.Equal("{\"a\":2}", stored.VariableValues);
            Assert.Equal("(x + 2)^2", stored.ExpectedAnswer);
        }

        [Fact]
        public async Task StartExerciseAsync_OpenExercise_IsReusedInsteadOfCreatingAnother()
        {
            var first = await _context.StartExerciseAsync(_formula.Id, 0);
            var second = await _context.StartExerciseAsync(_formula.Id, 0);

            Assert.Equal(first.ExerciseId, second.ExerciseId);
            Assert.Single(_practice.Exercises);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(2)]
        public async Task StartExerciseAsync_IndexOutsideTheList_ThrowsNotFoundException(int index)
        {
            await Assert.ThrowsAsync<NotFoundException>(() => _context.StartExerciseAsync(_formula.Id, index));
        }

        [Fact]
        public async Task StartExerciseAsync_Anonymous_ThrowsUnauthorizedException()
        {
            _currentUser.Anonymous();

            await Assert.ThrowsAsync<UnauthorizedException>(() => _context.StartExerciseAsync(_formula.Id, 0));
        }

        [Fact]
        public async Task SubmitAnswerAsync_CorrectAnswer_MarksSolvedAndRecordsTheAttempt()
        {
            var exercise = await _context.StartExerciseAsync(_formula.Id, 0);

            var result = await AnswerAsync(exercise.ExerciseId, "x^2 + 4x + 4");

            Assert.True(result.Correct);
            Assert.Equal("Correct", result.Verdict);
            Assert.Equal(new[] { 0 }, result.SolvedIndices);
            Assert.Equal(2, result.TotalExercises);
            Assert.Equal(_time.Now.UtcDateTime, _practice.Exercises.Single().SolvedAt);
            var submission = Assert.Single(_practice.Submissions);
            Assert.Equal(Student, submission.StudentWallet);
            Assert.Contains($"Formula {_formula.Id}, ExerciseIndex 0, Difficulty Easy, Solution x^2 + 4x + 4", submission.Content);
            Assert.Equal(10.0, Assert.Single(_practice.Grades).Score);
        }

        [Fact]
        public async Task SubmitAnswerAsync_WrongAnswer_UsesAnAttemptAndRecordsNothing()
        {
            var exercise = await _context.StartExerciseAsync(_formula.Id, 0);

            var result = await AnswerAsync(exercise.ExerciseId, "x^2 + 4");

            Assert.False(result.Correct);
            Assert.Equal("Incorrect", result.Verdict);
            Assert.Equal(PracticeContext.MaximumAttempts - 1, result.AttemptsLeft);
            Assert.Null(_practice.Exercises.Single().SolvedAt);
            Assert.Empty(_practice.Submissions);
        }

        [Fact]
        public async Task SubmitAnswerAsync_EquivalentButNotExpanded_IsNotSolved()
        {
            var exercise = await _context.StartExerciseAsync(_formula.Id, 0);

            var result = await AnswerAsync(exercise.ExerciseId, "(x + 2)^2");

            Assert.False(result.Correct);
            Assert.Equal("WrongForm", result.Verdict);
            Assert.Contains("not fully expanded", result.Message);
        }

        [Fact]
        public async Task SubmitAnswerAsync_UnreadableAnswer_DoesNotUseAnAttempt()
        {
            var exercise = await _context.StartExerciseAsync(_formula.Id, 0);

            var result = await AnswerAsync(exercise.ExerciseId, "x^2 +");

            Assert.Equal("Unreadable", result.Verdict);
            Assert.Equal(PracticeContext.MaximumAttempts, result.AttemptsLeft);
            Assert.Equal(0, _practice.Exercises.Single().Attempts);
        }

        [Fact]
        public async Task SubmitAnswerAsync_AfterTheLastAttempt_ClosesTheExerciseAndStartGivesANewOne()
        {
            var exercise = await _context.StartExerciseAsync(_formula.Id, 0);

            for (var attempt = 0; attempt < PracticeContext.MaximumAttempts; attempt++)
            {
                await AnswerAsync(exercise.ExerciseId, "x");
            }

            var exception = await Assert.ThrowsAsync<BusinessRuleException>(() => AnswerAsync(exercise.ExerciseId, "x^2 + 4x + 4"));
            Assert.Contains("every attempt", exception.Message);
            var next = await _context.StartExerciseAsync(_formula.Id, 0);
            Assert.NotEqual(exercise.ExerciseId, next.ExerciseId);
            Assert.Empty(_practice.Submissions);
        }

        [Fact]
        public async Task SubmitAnswerAsync_ExpiredExercise_ThrowsBusinessRuleException()
        {
            var exercise = await _context.StartExerciseAsync(_formula.Id, 0);
            _time.Now = _time.Now + PracticeContext.ExerciseLifetime;

            var exception = await Assert.ThrowsAsync<BusinessRuleException>(() => AnswerAsync(exercise.ExerciseId, "x^2 + 4x + 4"));

            Assert.Contains("expired", exception.Message);
            Assert.Empty(_practice.Submissions);
        }

        [Fact]
        public async Task SubmitAnswerAsync_LastAttemptsUsedByAParallelRequest_IsRejected()
        {
            var exercise = await _context.StartExerciseAsync(_formula.Id, 0);
            _practice.AfterRead = stored => stored.Attempts = PracticeContext.MaximumAttempts;

            var exception = await Assert.ThrowsAsync<BusinessRuleException>(() => AnswerAsync(exercise.ExerciseId, "x^2 + 4x + 4"));

            Assert.Contains("closed", exception.Message);
            Assert.Null(_practice.Exercises.Single().SolvedAt);
            Assert.Empty(_practice.Submissions);
        }

        [Fact]
        public async Task SubmitAnswerAsync_SameCorrectAnswerSentTwiceInParallel_IsRecordedOnce()
        {
            var exercise = await _context.StartExerciseAsync(_formula.Id, 0);
            _practice.AfterRegisterAttempt = stored => stored.SolvedAt = _time.Now.UtcDateTime;

            var result = await AnswerAsync(exercise.ExerciseId, "x^2 + 4x + 4");

            Assert.True(result.Correct);
            Assert.Empty(_practice.Submissions);
            Assert.Empty(_practice.Grades);
        }

        [Fact]
        public async Task SubmitAnswerAsync_SolvedExercise_CannotBeSolvedAgain()
        {
            var exercise = await _context.StartExerciseAsync(_formula.Id, 0);
            await AnswerAsync(exercise.ExerciseId, "x^2 + 4x + 4");

            await Assert.ThrowsAsync<BusinessRuleException>(() => AnswerAsync(exercise.ExerciseId, "x^2 + 4x + 4"));

            Assert.Single(_practice.Submissions);
        }

        [Fact]
        public async Task SubmitAnswerAsync_ExerciseOfAnotherStudent_ThrowsNotFoundException()
        {
            var exercise = await _context.StartExerciseAsync(_formula.Id, 0);
            _currentUser.As(OtherStudent, UserRole.Student);

            await Assert.ThrowsAsync<NotFoundException>(() => AnswerAsync(exercise.ExerciseId, "x^2 + 4x + 4"));

            Assert.Null(_practice.Exercises.Single().SolvedAt);
        }

        [Fact]
        public async Task SubmitAnswerAsync_UnknownExercise_ThrowsNotFoundException()
        {
            await Assert.ThrowsAsync<NotFoundException>(() => AnswerAsync(Guid.NewGuid(), "1"));
        }

        [Fact]
        public async Task GetFormulaAsync_ShowsOnlyTheCurrentStudentsProgress()
        {
            var exercise = await _context.StartExerciseAsync(_formula.Id, 1);
            await AnswerAsync(exercise.ExerciseId, "(x - 3)(x + 3)");

            Assert.Equal(new[] { 1 }, (await _context.GetFormulaAsync(_formula.Id)).SolvedIndices);

            _currentUser.As(OtherStudent, UserRole.Student);
            Assert.Empty((await _context.GetFormulaAsync(_formula.Id)).SolvedIndices);
        }
    }
}
