using MathChain.Application.DTOs;

namespace MathChain.Application.Contexts
{
    public interface IClassStatisticsContext
    {
        Task<ClassStatisticsDto> GetClassStatisticsAsync(Guid classRoomId);
    }
}
