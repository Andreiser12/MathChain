using MathChain.Domain.Entities;

namespace MathChain.Domain.Gateways
{
    public interface IGradeGateway
    {
        Task<Grade?> GetBySubmissionIdAsync(Guid submissionId);
        Task<List<Grade>> GetBySubmissionIdsAsync(List<Guid> submissionIds);
        Task<Grade> SaveAsync(Grade grade);
    }
}
