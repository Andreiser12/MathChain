using MathChain.Application.DTOs;

namespace MathChain.Application.Contexts
{
    public interface IDashboardContext
    {
        Task<DashboardStatsDto> GetDashboardStatsAsync(string? walletAddress);
        Task<RecordSolvedProblemResultDto> RecordSolvedProblemAsync(RecordSolvedProblemRequest request);
        Task<PracticeProgressDto> GetPracticeProgressAsync(string walletAddress, string formulaId);
    }
}
