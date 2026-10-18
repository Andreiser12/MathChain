using MathChain.Domain.Entities;

namespace MathChain.Domain.Gateways
{
    public interface IAssignmentGateway
    {
        Task<List<Assignment>> GetByClassRoomIdAsync(Guid classRoomId);
        Task<Assignment?> GetByIdAsync(Guid id);
        Task<Assignment> CreateAsync(Assignment assignment);
        Task DeleteAsync(Guid id);
    }
}
