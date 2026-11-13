using MathChain.Application.DTOs;

namespace MathChain.Application.Contexts
{
    public interface IDashboardContext
    {
        Task<DashboardStatsDto> GetDashboardStatsAsync();
        Task<RecordSolvedProblemResultDto> RecordSolvedProblemAsync(RecordSolvedProblemRequest request);
        Task<PracticeProgressDto> GetPracticeProgressAsync(string formulaId);
    }
}
