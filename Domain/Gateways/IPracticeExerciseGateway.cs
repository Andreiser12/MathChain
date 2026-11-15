using MathChain.Domain.Entities;

namespace MathChain.Domain.Gateways
{
    public interface IPracticeExerciseGateway
    {
        Task<PracticeExercise?> GetByIdAsync(Guid id);
        Task<PracticeExercise?> GetOpenAsync(string studentWallet, Guid formulaId, int exerciseIndex, DateTime now, int maximumAttempts);
        Task AddAsync(PracticeExercise exercise);
        Task<bool> TryRegisterAttemptAsync(Guid id, DateTime now, int maximumAttempts);
        Task<bool> TryMarkSolvedAsync(Guid id, DateTime solvedAt, Submission submission, Grade grade);
        Task<List<SolvedPracticeExercise>> GetSolvedAsync(string studentWallet, IReadOnlyCollection<Guid> formulaIds);
    }
}
