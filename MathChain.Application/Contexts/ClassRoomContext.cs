using MathChain.Application.DTOs;
using MathChain.Domain.Entities;
using MathChain.Domain.Enums;
using MathChain.Domain.Gateways;
using MathChain.Domain.Exceptions;

namespace MathChain.Application.Contexts
{
    public class ClassRoomContext : IClassRoomContext
    {
        private readonly IClassRoomGateway _classRoomGateway;
        private readonly AccessGuard _access;

        public ClassRoomContext(IClassRoomGateway classRoomGateway, AccessGuard access)
        {
            _classRoomGateway = classRoomGateway;
            _access = access;
        }

        private async Task<string> GenerateUniqueJoinCodeAsync()
        {
            const string allowedChars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var random = new Random();
            string code;
            bool exists;

            do
            {
                code = new string(Enumerable.Range(0, 6)
                    .Select(_ => allowedChars[random.Next(allowedChars.Length)])
                    .ToArray());
                exists = await _classRoomGateway.IsJoinCodeTakenAsync(code);
            } while (exists);

            return code;
        }

        public async Task<ClassRoom> CreateClassRoomAsync(CreateClassRoomRequest request)
        {
            var teacherWallet = _access.RequireRole(UserRole.Teacher);

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new BusinessRuleException("Classroom name is required.");
            }

            var classRoom = new ClassRoom
            {
                Id = Guid.NewGuid(),
                Name = request.Name.Trim(),
                TeacherWallet = teacherWallet,
                JoinCode = await GenerateUniqueJoinCodeAsync(),
                CreatedAt = DateTime.UtcNow
            };

            var created = await _classRoomGateway.CreateAsync(classRoom);

            return created;
        }

        public async Task<ClassRoom> JoinClassRoomAsync(JoinClassRoomRequest request)
        {
            var studentWallet = _access.RequireRole(UserRole.Student);

            if (string.IsNullOrWhiteSpace(request.JoinCode))
            {
                throw new BusinessRuleException("Join code is required.");
            }

            var joinCode = request.JoinCode.Trim().ToUpper();
            var classRoom = await _classRoomGateway.GetByJoinCodeAsync(joinCode);

            if (classRoom == null)
            {
                throw new NotFoundException("Classroom not found for the provided join code.");
            }

            var alreadyEnrolled = await _classRoomGateway.IsStudentEnrolledAsync(classRoom.Id, studentWallet);

            if (alreadyEnrolled)
            {
                throw new BusinessRuleException("Student is already enrolled in this classroom.");
            }

            var enrollment = new Enrollment
            {
                Id = Guid.NewGuid(),
                ClassRoomId = classRoom.Id,
                StudentWallet = studentWallet,
                CreatedAt = DateTime.UtcNow
            };

            await _classRoomGateway.EnrollStudentAsync(enrollment);

            return classRoom;
        }

        public async Task<ClassRoom> GetClassRoomByIdAsync(Guid id)
        {
            return await _access.RequireMemberAsync(id);
        }

        public async Task<List<ClassRoom>> GetByTeacherWalletAsync(string teacherWallet)
        {
            if (string.IsNullOrWhiteSpace(teacherWallet))
            {
                throw new BusinessRuleException("Teacher wallet address is required.");
            }

            _access.RequireSelfOrAdmin(teacherWallet);

            var classRooms = await _classRoomGateway.GetByTeacherWalletAsync(teacherWallet.Trim().ToLower());

            return classRooms;
        }

        public async Task<List<ClassRoom>> GetByStudentWalletAsync(string studentWallet)
        {
            if (string.IsNullOrWhiteSpace(studentWallet))
            {
                throw new BusinessRuleException("Student wallet address is required.");
            }

            _access.RequireSelfOrAdmin(studentWallet);

            var classRooms = await _classRoomGateway.GetEnrolledByStudentWalletAsync(studentWallet.Trim().ToLower());

            return classRooms;
        }

        public async Task<List<string>> GetEnrolledStudentsAsync(Guid classRoomId)
        {
            await _access.RequireOwnerAsync(classRoomId);

            var students = await _classRoomGateway.GetEnrolledStudentsWalletsAsync(classRoomId);

            return students;
        }

        public async Task DeleteClassRoomAsync(Guid id)
        {
            await _access.RequireOwnerAsync(id);

            await _classRoomGateway.DeleteAsync(id);
        }
    }
}
