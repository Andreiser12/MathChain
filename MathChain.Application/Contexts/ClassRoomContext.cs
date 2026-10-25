using MathChain.Application.DTOs;
using MathChain.Domain.Entities;
using MathChain.Domain.Gateways;
using MathChain.Infrastructure.ResponseHandling;

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

        public async Task<Envelope<ClassRoom>> CreateClassRoomAsync(CreateClassRoomRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.TeacherWallet))
            {
                return EnvelopeExtensions.Failure<ClassRoom>("Classroom name and teacher wallet are required.", 400);
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

            return EnvelopeExtensions.Success(created, "Classroom created successfully.", 201);
        }

        public async Task<Envelope<ClassRoom>> JoinClassRoomAsync(JoinClassRoomRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.JoinCode) || string.IsNullOrWhiteSpace(request.StudentWallet))
            {
                return EnvelopeExtensions.Failure<ClassRoom>("Join code and student wallet are required.", 400);
            }

            var joinCode = request.JoinCode.Trim().ToUpper();
            var studentWallet = request.StudentWallet.Trim().ToLower();
            var classRoom = await _classRoomGateway.GetByJoinCodeAsync(joinCode);

            if (classRoom == null)
            {
                return EnvelopeExtensions.Failure<ClassRoom>("Classroom not found for the provided join code.", 404);
            }

            var alreadyEnrolled = await _classRoomGateway.IsStudentEnrolledAsync(classRoom.Id, studentWallet);

            if (alreadyEnrolled)
            {
                return EnvelopeExtensions.Failure<ClassRoom>("Student is already enrolled in this classroom.", 400);
            }

            var enrollment = new Enrollment
            {
                Id = Guid.NewGuid(),
                ClassRoomId = classRoom.Id,
                StudentWallet = studentWallet,
                CreatedAt = DateTime.UtcNow
            };

            await _classRoomGateway.EnrollStudentAsync(enrollment);

            return EnvelopeExtensions.Success(classRoom, "Enrolled in classroom successfully.");
        }

        public async Task<Envelope<ClassRoom>> GetClassRoomByIdAsync(Guid id)
        {
            var classRoom = await _classRoomGateway.GetByIdAsync(id);

            if (classRoom == null)
            {
                return EnvelopeExtensions.Failure<ClassRoom>("Classroom not found.", 404);
            }

            return EnvelopeExtensions.Success(classRoom);
        }

        public async Task<Envelope<List<ClassRoom>>> GetByTeacherWalletAsync(string teacherWallet)
        {
            if (string.IsNullOrWhiteSpace(teacherWallet))
            {
                return EnvelopeExtensions.Failure<List<ClassRoom>>("Teacher wallet address is required.", 400);
            }

            var classRooms = await _classRoomGateway.GetByTeacherWalletAsync(teacherWallet.Trim().ToLower());

            return EnvelopeExtensions.Success(classRooms);
        }

        public async Task<Envelope<List<ClassRoom>>> GetByStudentWalletAsync(string studentWallet)
        {
            if (string.IsNullOrWhiteSpace(studentWallet))
            {
                return EnvelopeExtensions.Failure<List<ClassRoom>>("Student wallet address is required.", 400);
            }

            var classRooms = await _classRoomGateway.GetEnrolledByStudentWalletAsync(studentWallet.Trim().ToLower());

            return EnvelopeExtensions.Success(classRooms);
        }

        public async Task<Envelope<List<string>>> GetEnrolledStudentsAsync(Guid classRoomId)
        {
            var students = await _classRoomGateway.GetEnrolledStudentsWalletsAsync(classRoomId);

            return EnvelopeExtensions.Success(students);
        }

        public async Task<Envelope<object>> DeleteClassRoomAsync(Guid id)
        {
            var classRoom = await _classRoomGateway.GetByIdAsync(id);

            if (classRoom == null)
            {
                return EnvelopeExtensions.Failure("Classroom not found.", 404);
            }
            await _classRoomGateway.DeleteAsync(id);
            return EnvelopeExtensions.Success("Classroom deleted successfully.", 200);
        }
    }
}
