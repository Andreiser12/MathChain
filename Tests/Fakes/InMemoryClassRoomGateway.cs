using MathChain.Domain.Entities;
using MathChain.Domain.Gateways;

namespace MathChain.Tests.Fakes
{
    public class InMemoryClassRoomGateway : IClassRoomGateway
    {
        public List<ClassRoom> ClassRooms { get; } = new();
        public List<Enrollment> Enrollments { get; } = new();

        public Task<ClassRoom?> GetByIdAsync(Guid id)
        {
            return Task.FromResult(ClassRooms.FirstOrDefault(c => c.Id == id));
        }

        public Task<ClassRoom?> GetByJoinCodeAsync(string joinCode)
        {
            return Task.FromResult(ClassRooms.FirstOrDefault(c => c.JoinCode == joinCode));
        }

        public Task<List<ClassRoom>> GetByTeacherWalletAsync(string teacherWallet)
        {
            return Task.FromResult(ClassRooms.Where(c => c.TeacherWallet == teacherWallet).ToList());
        }

        public Task<List<ClassRoom>> GetEnrolledByStudentWalletAsync(string studentWallet)
        {
            var classRoomIds = Enrollments.Where(e => e.StudentWallet == studentWallet).Select(e => e.ClassRoomId).ToHashSet();
            return Task.FromResult(ClassRooms.Where(c => classRoomIds.Contains(c.Id)).ToList());
        }

        public Task<List<string>> GetEnrolledStudentsWalletsAsync(Guid classRoomId)
        {
            return Task.FromResult(Enrollments.Where(e => e.ClassRoomId == classRoomId).Select(e => e.StudentWallet).ToList());
        }

        public Task<bool> IsJoinCodeTakenAsync(string joinCode)
        {
            return Task.FromResult(ClassRooms.Any(c => c.JoinCode == joinCode));
        }

        public Task<bool> IsStudentEnrolledAsync(Guid classRoomId, string studentWallet)
        {
            return Task.FromResult(Enrollments.Any(e => e.ClassRoomId == classRoomId && e.StudentWallet == studentWallet));
        }

        public Task<ClassRoom> CreateAsync(ClassRoom classRoom)
        {
            ClassRooms.Add(classRoom);
            return Task.FromResult(classRoom);
        }

        public Task EnrollStudentAsync(Enrollment enrollment)
        {
            Enrollments.Add(enrollment);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid id)
        {
            ClassRooms.RemoveAll(c => c.Id == id);
            return Task.CompletedTask;
        }
    }
}
