using MathChain.Application.DTOs;
using MathChain.Domain.Entities;
using MathChain.Infrastructure.ResponseHandling;

namespace MathChain.Application.Contexts
{
    public interface IAssignmentContext
    {
        Task<Envelope<List<AssignmentResponseDto>>> GetAssignmentsByClassRoomAsync(Guid classRoomId);
        Task<Envelope<Assignment>> GetAssignmentByIdAsync(Guid id);
        Task<Envelope<Assignment>> CreateAssignmentAsync(Guid classRoomId, CreateAssignmentRequest request);
        Task<Envelope<object>> DeleteAssignmentAsync(Guid id);
        Task<Envelope<List<SubmissionResponseDto>>> GetSubmissionsAsync(Guid assignmentId);
        Task<Envelope<Submission>> SubmitAssignmentAsync(Guid assignmentId, SubmitAssignmentRequest request);
        Task<Envelope<Grade>> GradeSubmissionAsync(Guid submissionId, GradeSubmissionRequest request);
    }
}
