using MathChain.Application.DTOs;
using MathChain.Domain.Entities;

namespace MathChain.Application.Contexts
{
    public interface IAssignmentContext
    {
        Task<List<AssignmentResponseDto>> GetAssignmentsByClassRoomAsync(Guid classRoomId);
        Task<Assignment> GetAssignmentByIdAsync(Guid id);
        Task<Assignment> CreateAssignmentAsync(Guid classRoomId, CreateAssignmentRequest request);
        Task DeleteAssignmentAsync(Guid id);
        Task<List<SubmissionResponseDto>> GetSubmissionsAsync(Guid assignmentId);
        Task<Submission> SubmitAssignmentAsync(Guid assignmentId, SubmitAssignmentRequest request);
        Task<Grade> GradeSubmissionAsync(Guid submissionId, GradeSubmissionRequest request);
    }
}
