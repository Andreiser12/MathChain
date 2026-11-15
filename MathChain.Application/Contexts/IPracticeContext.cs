using MathChain.Application.DTOs;

namespace MathChain.Application.Contexts
{
    public interface IPracticeContext
    {
        Task<PracticeFormulaDto> GetFormulaAsync(Guid formulaId);
        Task<PracticeExerciseDto> StartExerciseAsync(Guid formulaId, int index);
        Task<PracticeAnswerResultDto> SubmitAnswerAsync(Guid exerciseId, SubmitPracticeAnswerRequest request);
    }
}
