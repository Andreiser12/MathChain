using MathChain.Domain.Entities;
using MathChain.Domain.Gateways;

namespace MathChain.Tests.Shared.Fakes
{
    public class InMemoryPracticeExerciseGateway : IPracticeExerciseGateway
    {
        public List<PracticeExercise> Exercises { get; } = new();
        public List<Submission> Submissions { get; } = new();
        public List<Grade> Grades { get; } = new();
        public Action<PracticeExercise>? AfterRead { get; set; }
        public Action<PracticeExercise>? AfterRegisterAttempt { get; set; }

        public Task<PracticeExercise?> GetByIdAsync(Guid id)
        {
            var copy = EntityCopy.Detached(Exercises.FirstOrDefault(e => e.Id == id));

            if (copy != null)
            {
                AfterRead?.Invoke(Exercises.First(e => e.Id == id));
            }

            return Task.FromResult(copy);
        }

        public Task<PracticeExercise?> GetOpenAsync(string studentWallet, Guid formulaId, int exerciseIndex, DateTime now, int maximumAttempts)
        {
            return Task.FromResult(EntityCopy.Detached(Exercises
                .Where(e => e.StudentWallet == studentWallet && e.FormulaId == formulaId && e.ExerciseIndex == exerciseIndex
                    && e.SolvedAt == null && e.ExpiresAt > now && e.Attempts < maximumAttempts)
                .OrderByDescending(e => e.CreatedAt)
                .FirstOrDefault()));
        }

        public Task AddAsync(PracticeExercise exercise)
        {
            Exercises.Add(EntityCopy.Detached(exercise)!);
            return Task.CompletedTask;
        }

        public Task<bool> TryRegisterAttemptAsync(Guid id, DateTime now, int maximumAttempts)
        {
            var exercise = Exercises.FirstOrDefault(e => e.Id == id && e.SolvedAt == null && e.ExpiresAt > now && e.Attempts < maximumAttempts);

            if (exercise == null)
            {
                return Task.FromResult(false);
            }

            exercise.Attempts++;
            AfterRegisterAttempt?.Invoke(exercise);
            return Task.FromResult(true);
        }

        public Task<bool> TryMarkSolvedAsync(Guid id, DateTime solvedAt, Submission submission, Grade grade)
        {
            var exercise = Exercises.FirstOrDefault(e => e.Id == id && e.SolvedAt == null);

            if (exercise == null)
            {
                return Task.FromResult(false);
            }

            exercise.SolvedAt = solvedAt;
            Submissions.Add(submission);
            Grades.Add(grade);
            return Task.FromResult(true);
        }

        public Task<List<SolvedPracticeExercise>> GetSolvedAsync(string studentWallet, IReadOnlyCollection<Guid> formulaIds)
        {
            return Task.FromResult(Exercises
                .Where(e => e.StudentWallet == studentWallet && formulaIds.Contains(e.FormulaId) && e.SolvedAt != null)
                .Select(e => (e.FormulaId, e.ExerciseIndex))
                .Distinct()
                .Select(e => new SolvedPracticeExercise { FormulaId = e.FormulaId, ExerciseIndex = e.ExerciseIndex })
                .ToList());
        }

        public void MarkSolved(string studentWallet, Guid formulaId, int exerciseIndex)
        {
            Exercises.Add(new PracticeExercise
            {
                Id = Guid.NewGuid(),
                StudentWallet = studentWallet,
                FormulaId = formulaId,
                ExerciseIndex = exerciseIndex,
                ExpectedAnswer = "0",
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                SolvedAt = DateTime.UtcNow
            });
        }
    }
}
