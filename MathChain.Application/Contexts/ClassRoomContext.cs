using MathChain.Application.DTOs;
using MathChain.Domain.Entities;
using MathChain.Domain.Gateways;
using MathChain.Domain.Exceptions;

namespace MathChain.Application.Contexts
{
    public class ClassRoomContext : IClassRoomContext
    {
        private readonly IClassRoomGateway _classRoomGateway;
        public ClassRoomContext(IClassRoomGateway classRoomGateway)
        {
            _classRoomGateway = classRoomGateway;
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
            if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.TeacherWallet))
            {
                throw new BusinessRuleException("Classroom name and teacher wallet are required.");
            }

            var classRoom = new ClassRoom
            {
                Id = Guid.NewGuid(),
                Name = request.Name.Trim(),
                TeacherWallet = request.TeacherWallet.Trim().ToLower(),
                JoinCode = await GenerateUniqueJoinCodeAsync(),
                CreatedAt = DateTime.UtcNow
            };

            var created = await _classRoomGateway.CreateAsync(classRoom);

            return created;
        }

        public async Task<ClassRoom> JoinClassRoomAsync(JoinClassRoomRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.JoinCode) || string.IsNullOrWhiteSpace(request.StudentWallet))
            {
                throw new BusinessRuleException("Join code and student wallet are required.");
            }

            var joinCode = request.JoinCode.Trim().ToUpper();
            var studentWallet = request.StudentWallet.Trim().ToLower();
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
            var classRoom = await _classRoomGateway.GetByIdAsync(id);

            if (classRoom == null)
            {
                throw new NotFoundException("Classroom not found.");
            }

            return classRoom;
        }

        public async Task<List<ClassRoom>> GetByTeacherWalletAsync(string teacherWallet)
        {
            if (string.IsNullOrWhiteSpace(teacherWallet))
            {
                throw new BusinessRuleException("Teacher wallet address is required.");
            }

            var classRooms = await _classRoomGateway.GetByTeacherWalletAsync(teacherWallet.Trim().ToLower());

            return classRooms;
        }

        public async Task<List<ClassRoom>> GetByStudentWalletAsync(string studentWallet)
        {
            if (string.IsNullOrWhiteSpace(studentWallet))
            {
                throw new BusinessRuleException("Student wallet address is required.");
            }

            var classRooms = await _classRoomGateway.GetEnrolledByStudentWalletAsync(studentWallet.Trim().ToLower());

            return classRooms;
        }

        public async Task<List<string>> GetEnrolledStudentsAsync(Guid classRoomId)
        {
            var students = await _classRoomGateway.GetEnrolledStudentsWalletsAsync(classRoomId);

            return students;
        }

        public async Task DeleteClassRoomAsync(Guid id)
        {
            var classRoom = await _classRoomGateway.GetByIdAsync(id);

            if (classRoom == null)
            {
                throw new NotFoundException("Classroom not found.");
            }
            await _classRoomGateway.DeleteAsync(id);
        }
    }
}
