using MathChain.Application.DTOs;

namespace MathChain.Application.Contexts
{
    public interface IDashboardContext
    {
        Task<DashboardStatsDto> GetDashboardStatsAsync();
    }
}
