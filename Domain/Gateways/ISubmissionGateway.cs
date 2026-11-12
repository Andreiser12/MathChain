using MathChain.Domain.Entities;

namespace MathChain.Domain.Gateways
{
    public interface ISubmissionGateway
    {
        Task<List<Submission>> GetByAssignmentIdAsync(Guid assignmentId);
        Task<Submission?> GetByIdAsync(Guid id);
        Task<Submission?> GetByStudentAndAssignmentAsync(Guid assignmentId, string studentWallet);
        Task<Submission> SaveAsync(Submission submission);
        Task<List<Submission>> GetAllAsync();
        Task<List<Submission>> GetByAssignmentIdsAsync(List<Guid> assignmentIds);
        Task<List<Submission>> GetPracticeSubmissionsAsync(string studentWallet, string formulaId);
        Task<int> CountByStudentWalletAsync(string studentWallet);
    }
}
