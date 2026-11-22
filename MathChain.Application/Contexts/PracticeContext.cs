using System.Text.Json;
using MathChain.Application.DTOs;
using MathChain.Application.Practice;
using MathChain.Domain.Entities;
using MathChain.Domain.Exceptions;
using MathChain.Domain.Gateways;

namespace MathChain.Application.Contexts
{
    public class PracticeContext : IPracticeContext
    {
        public const int MaximumAttempts = 10;
        public static readonly TimeSpan ExerciseLifetime = TimeSpan.FromHours(2);

        private const int MaximumStoredAnswerLength = 300;
        private const string ExerciseNotFoundMessage = "Exercise not found.";

        private readonly ICurriculumGateway _curriculumGateway;
        private readonly IPracticeExerciseGateway _exerciseGateway;
        private readonly AccessGuard _access;
        private readonly TimeProvider _timeProvider;
        private readonly PracticeRandom _random;

        public PracticeContext(ICurriculumGateway curriculumGateway, IPracticeExerciseGateway exerciseGateway,
            AccessGuard access, TimeProvider timeProvider, PracticeRandom random)
        {
            _curriculumGateway = curriculumGateway;
            _exerciseGateway = exerciseGateway;
            _access = access;
            _timeProvider = timeProvider;
            _random = random;
        }

        public async Task<PracticeFormulaDto> GetFormulaAsync(Guid formulaId)
        {
            var wallet = _access.RequireWallet();
            var formula = await _curriculumGateway.GetFormulaByIdAsync(formulaId);
            var exercises = ExerciseCatalog.ForFormula(formula?.ExercisesJson);

            return new PracticeFormulaDto
            {
                FormulaId = formulaId,
                Title = formula?.Title ?? string.Empty,
                UsesGenericExercises = exercises.UsesGenericExercises,
                Exercises = exercises.Exercises
                    .Select(e => new PracticeExerciseSummaryDto { Index = e.Index, Difficulty = e.Difficulty, Form = e.Form })
                    .ToList(),
                SolvedIndices = await GetSolvedIndicesAsync(wallet, formulaId, exercises.Exercises.Count)
            };
        }

        public async Task<PracticeExerciseDto> StartExerciseAsync(Guid formulaId, int index)
        {
            var wallet = _access.RequireWallet();
            var formula = await _curriculumGateway.GetFormulaByIdAsync(formulaId);
            var exercises = ExerciseCatalog.ForFormula(formula?.ExercisesJson);

            if (index < 0 || index >= exercises.Exercises.Count)
            {
                throw new NotFoundException(ExerciseNotFoundMessage);
            }

            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var exercise = await _exerciseGateway.GetOpenAsync(wallet, formulaId, index, now, MaximumAttempts);

            if (exercise == null)
            {
                var definition = exercises.Exercises[index];
                GeneratedExercise generated;

                try
                {
                    generated = ExerciseGenerator.Generate(definition.Template, _random);
                }
                catch (MathExpressionParseException)
                {
                    throw new BusinessRuleException("This exercise could not be prepared. Try again.");
                }

                exercise = new PracticeExercise
                {
                    Id = Guid.NewGuid(),
                    StudentWallet = wallet,
                    FormulaId = formulaId,
                    ExerciseIndex = index,
                    Difficulty = definition.Difficulty,
                    Requirement = generated.Requirement,
                    Latex = generated.Latex,
                    Hint = generated.Hint,
                    Form = generated.Form,
                    ExpectedAnswer = generated.ExpectedAnswer,
                    VariableValues = JsonSerializer.Serialize(generated.Values),
                    Attempts = 0,
                    CreatedAt = now,
                    ExpiresAt = now + ExerciseLifetime
                };

                await _exerciseGateway.AddAsync(exercise);
            }

            return new PracticeExerciseDto
            {
                ExerciseId = exercise.Id,
                FormulaId = exercise.FormulaId,
                Index = exercise.ExerciseIndex,
                Difficulty = exercise.Difficulty,
                Requirement = exercise.Requirement,
                Latex = exercise.Latex,
                Hint = exercise.Hint,
                Form = exercise.Form,
                AttemptsLeft = MaximumAttempts - exercise.Attempts,
                ExpiresAt = exercise.ExpiresAt
            };
        }

        public async Task<PracticeAnswerResultDto> SubmitAnswerAsync(Guid exerciseId, SubmitPracticeAnswerRequest request)
        {
            var wallet = _access.RequireWallet();
            var exercise = await _exerciseGateway.GetByIdAsync(exerciseId);

            if (exercise == null || exercise.StudentWallet != wallet)
            {
                throw new NotFoundException(ExerciseNotFoundMessage);
            }

            var now = _timeProvider.GetUtcNow().UtcDateTime;
            EnsureOpen(exercise, now);

            var check = AnswerChecker.Check(exercise.Form, exercise.ExpectedAnswer, request?.Answer);

            if (check.Verdict == AnswerVerdict.Unreadable)
            {
                return await BuildResultAsync(exercise, false, check, MaximumAttempts - exercise.Attempts);
            }

            if (!await _exerciseGateway.TryRegisterAttemptAsync(exercise.Id, now, MaximumAttempts))
            {
                throw new BusinessRuleException("This exercise is closed. Load a new exercise.");
            }

            var attemptsLeft = MaximumAttempts - exercise.Attempts - 1;

            if (check.Verdict != AnswerVerdict.Correct)
            {
                return await BuildResultAsync(exercise, false, check, attemptsLeft);
            }

            var answer = request!.Answer.Trim();
            var submission = new Submission
            {
                Id = Guid.NewGuid(),
                AssignmentId = Guid.Empty,
                StudentWallet = wallet,
                Content = $"Practice: Formula {exercise.FormulaId}, ExerciseIndex {exercise.ExerciseIndex}, Difficulty {exercise.Difficulty}, Solution {(answer.Length > MaximumStoredAnswerLength ? answer[..MaximumStoredAnswerLength] : answer)}",
                SubmittedAt = now
            };
            var grade = new Grade
            {
                Id = Guid.NewGuid(),
                SubmissionId = submission.Id,
                Score = 10.0,
                Feedback = "Verified correct in Practice Arena",
                GradedAt = now
            };

            await _exerciseGateway.TryMarkSolvedAsync(exercise.Id, now, submission, grade);

            return await BuildResultAsync(exercise, true, check, attemptsLeft);
        }

        private static void EnsureOpen(PracticeExercise exercise, DateTime now)
        {
            if (exercise.SolvedAt != null)
            {
                throw new BusinessRuleException("This exercise is already solved. Load the next exercise.");
            }

            if (exercise.ExpiresAt <= now)
            {
                throw new BusinessRuleException("This exercise has expired. Load a new exercise.");
            }

            if (exercise.Attempts >= MaximumAttempts)
            {
                throw new BusinessRuleException("You have used every attempt for this exercise. Load a new exercise.");
            }
        }

        private async Task<PracticeAnswerResultDto> BuildResultAsync(PracticeExercise exercise, bool correct, AnswerCheck check, int attemptsLeft)
        {
            var formula = await _curriculumGateway.GetFormulaByIdAsync(exercise.FormulaId);
            var total = ExerciseCatalog.ForFormula(formula?.ExercisesJson).Exercises.Count;

            return new PracticeAnswerResultDto
            {
                Correct = correct,
                Verdict = check.Verdict.ToString(),
                Message = check.Message,
                AttemptsLeft = Math.Max(0, attemptsLeft),
                SolvedIndices = await GetSolvedIndicesAsync(exercise.StudentWallet, exercise.FormulaId, total),
                TotalExercises = total
            };
        }

        private async Task<List<int>> GetSolvedIndicesAsync(string wallet, Guid formulaId, int exerciseCount)
        {
            var solved = await _exerciseGateway.GetSolvedAsync(wallet, new[] { formulaId });

            return solved
                .Where(s => s.ExerciseIndex < exerciseCount)
                .Select(s => s.ExerciseIndex)
                .Distinct()
                .OrderBy(i => i)
                .ToList();
        }
    }
}
