using MathChain.Domain.Entities;

namespace MathChain.Domain.Gateways
{
    public interface IClassRoomGateway
    {
        Task<ClassRoom?> GetByIdAsync(Guid id);
        Task<ClassRoom?> GetByJoinCodeAsync(string joinCode);
        Task<List<ClassRoom>> GetByTeacherWalletAsync(string teacherWallet);
        Task<List<ClassRoom>> GetEnrolledByStudentWalletAsync(string studentWallet);
        Task<List<string>> getenrolledStudentsWallets(Guid classRoomId);
        Task<bool> IsJoinCodeTakenAsync(string code);
        Task<bool> IsStudentEnrolledAsync(Guid classRoomId, string studentWallet);
        Task<ClassRoom> CreateAsync(ClassRoom classRoom);
        Task EnrollStudentAsync(Enrollment enrollment);
        Task DeleteAsync(Guid id);
    }
}
