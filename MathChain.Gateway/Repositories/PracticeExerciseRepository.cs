using MathChain.Domain.Entities;
using MathChain.Domain.Gateways;
using MathChain.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace MathChain.Gateway.Repositories
{
    public class PracticeExerciseRepository : BaseRepository, IPracticeExerciseGateway
    {
        public PracticeExerciseRepository(MathChainDbContext context) : base(context)
        {
        }

        public async Task<PracticeExercise?> GetByIdAsync(Guid id)
        {
            return await _context.PracticeExercises
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<PracticeExercise?> GetOpenAsync(string studentWallet, Guid formulaId, int exerciseIndex, DateTime now, int maximumAttempts)
        {
            return await _context.PracticeExercises
                .AsNoTracking()
                .Where(e => e.StudentWallet == studentWallet
                    && e.FormulaId == formulaId
                    && e.ExerciseIndex == exerciseIndex
                    && e.SolvedAt == null
                    && e.ExpiresAt > now
                    && e.Attempts < maximumAttempts)
                .OrderByDescending(e => e.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task AddAsync(PracticeExercise exercise)
        {
            _context.PracticeExercises.Add(exercise);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();
        }

        public async Task<bool> TryRegisterAttemptAsync(Guid id, DateTime now, int maximumAttempts)
        {
            var updated = await _context.PracticeExercises
                .Where(e => e.Id == id && e.SolvedAt == null && e.ExpiresAt > now && e.Attempts < maximumAttempts)
                .ExecuteUpdateAsync(setters => setters.SetProperty(e => e.Attempts, e => e.Attempts + 1));

            return updated == 1;
        }

        public async Task<bool> TryMarkSolvedAsync(Guid id, DateTime solvedAt, Submission submission, Grade grade)
        {
            var updated = await _context.PracticeExercises
                .Where(e => e.Id == id && e.SolvedAt == null)
                .ExecuteUpdateAsync(setters => setters.SetProperty(e => e.SolvedAt, solvedAt));

            if (updated != 1)
            {
                return false;
            }

            _context.Submissions.Add(submission);
            _context.Grades.Add(grade);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            return true;
        }

        public async Task<List<SolvedPracticeExercise>> GetSolvedAsync(string studentWallet, IReadOnlyCollection<Guid> formulaIds)
        {
            return await _context.PracticeExercises
                .AsNoTracking()
                .Where(e => e.StudentWallet == studentWallet && formulaIds.Contains(e.FormulaId) && e.SolvedAt != null)
                .Select(e => new SolvedPracticeExercise { FormulaId = e.FormulaId, ExerciseIndex = e.ExerciseIndex })
                .Distinct()
                .ToListAsync();
        }
    }
}
