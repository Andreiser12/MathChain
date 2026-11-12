using MathChain.Application.DTOs;

namespace MathChain.Application.Contexts
{
    public interface IGradebookContext
    {
        Task<List<StudentGradebookDto>> GetGradebookAsync(Guid classRoomId);
        Task SetFinalGradeAsync(Guid classRoomId, string studentWallet, int finalGrade);
        Task RemoveStudentAsync(Guid classRoomId, string studentWallet);
    }
}
