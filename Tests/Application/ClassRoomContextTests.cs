using MathChain.Application.Contexts;
using MathChain.Application.DTOs;
using MathChain.Domain.Entities;
using MathChain.Domain.Enums;
using MathChain.Domain.Exceptions;
using MathChain.Tests.Fakes;

namespace MathChain.Tests.Application
{
    public class ClassRoomContextTests
    {
        private const string AllowedJoinCodeChars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        private const string Teacher = "0xteacher";
        private const string OtherTeacher = "0xotherteacher";
        private const string Student = "0xstudent";

        private readonly InMemoryClassRoomGateway _gateway = new();
        private readonly FakeCurrentUser _currentUser = new();
        private readonly ClassRoomContext _context;

        public ClassRoomContextTests()
        {
            _context = new ClassRoomContext(_gateway, new AccessGuard(_currentUser, _gateway));
        }

        private ClassRoom AddClassRoom(string joinCode = "ABC234", string teacherWallet = Teacher)
        {
            var classRoom = new ClassRoom
            {
                Id = Guid.NewGuid(),
                Name = "Algebra",
                TeacherWallet = teacherWallet,
                JoinCode = joinCode,
                CreatedAt = DateTime.UtcNow
            };
            _gateway.ClassRooms.Add(classRoom);
            return classRoom;
        }

        private void Enroll(ClassRoom classRoom, string studentWallet)
        {
            _gateway.Enrollments.Add(new Enrollment { Id = Guid.NewGuid(), ClassRoomId = classRoom.Id, StudentWallet = studentWallet });
        }

        [Fact]
        public async Task CreateClassRoomAsync_Teacher_OwnsClassRoomRegardlessOfRequestWallet()
        {
            _currentUser.As("0xTEACHER", UserRole.Teacher);

            var classRoom = await _context.CreateClassRoomAsync(new CreateClassRoomRequest { Name = "  Algebra  ", TeacherWallet = "0xsomeoneelse" });

            Assert.Equal("Algebra", classRoom.Name);
            Assert.Equal(Teacher, classRoom.TeacherWallet);
            Assert.Equal(6, classRoom.JoinCode.Length);
            Assert.All(classRoom.JoinCode, c => Assert.Contains(c, AllowedJoinCodeChars));
        }

        [Fact]
        public async Task CreateClassRoomAsync_Student_ThrowsForbiddenException()
        {
            _currentUser.As(Student, UserRole.Student);

            await Assert.ThrowsAsync<ForbiddenException>(() => _context.CreateClassRoomAsync(new CreateClassRoomRequest { Name = "Algebra" }));
            Assert.Empty(_gateway.ClassRooms);
        }

        [Fact]
        public async Task CreateClassRoomAsync_Anonymous_ThrowsUnauthorizedException()
        {
            _currentUser.Anonymous();

            await Assert.ThrowsAsync<UnauthorizedException>(() => _context.CreateClassRoomAsync(new CreateClassRoomRequest { Name = "Algebra" }));
        }

        [Fact]
        public async Task CreateClassRoomAsync_MissingName_ThrowsBusinessRuleException()
        {
            _currentUser.As(Teacher, UserRole.Teacher);

            await Assert.ThrowsAsync<BusinessRuleException>(() => _context.CreateClassRoomAsync(new CreateClassRoomRequest { Name = "" }));
        }

        [Fact]
        public async Task JoinClassRoomAsync_Student_EnrollsWalletFromToken()
        {
            var classRoom = AddClassRoom("ABC234");
            _currentUser.As("0xSTUDENT", UserRole.Student);

            var joined = await _context.JoinClassRoomAsync(new JoinClassRoomRequest { JoinCode = " abc234 ", StudentWallet = "0xvictim" });

            Assert.Equal(classRoom.Id, joined.Id);
            Assert.Equal(Student, Assert.Single(_gateway.Enrollments).StudentWallet);
        }

        [Fact]
        public async Task JoinClassRoomAsync_Teacher_ThrowsForbiddenException()
        {
            AddClassRoom("ABC234");
            _currentUser.As(OtherTeacher, UserRole.Teacher);

            await Assert.ThrowsAsync<ForbiddenException>(() => _context.JoinClassRoomAsync(new JoinClassRoomRequest { JoinCode = "ABC234" }));
        }

        [Fact]
        public async Task JoinClassRoomAsync_UnknownCode_ThrowsNotFoundException()
        {
            AddClassRoom("ABC234");
            _currentUser.As(Student, UserRole.Student);

            await Assert.ThrowsAsync<NotFoundException>(() => _context.JoinClassRoomAsync(new JoinClassRoomRequest { JoinCode = "ZZZZZZ" }));
        }

        [Fact]
        public async Task JoinClassRoomAsync_AlreadyEnrolled_ThrowsBusinessRuleException()
        {
            AddClassRoom("ABC234");
            _currentUser.As(Student, UserRole.Student);
            await _context.JoinClassRoomAsync(new JoinClassRoomRequest { JoinCode = "ABC234" });

            await Assert.ThrowsAsync<BusinessRuleException>(() => _context.JoinClassRoomAsync(new JoinClassRoomRequest { JoinCode = "ABC234" }));
            Assert.Single(_gateway.Enrollments);
        }

        [Fact]
        public async Task GetClassRoomByIdAsync_EnrolledStudent_ReturnsClassRoom()
        {
            var classRoom = AddClassRoom();
            Enroll(classRoom, Student);
            _currentUser.As(Student, UserRole.Student);

            var result = await _context.GetClassRoomByIdAsync(classRoom.Id);

            Assert.Equal(classRoom.Id, result.Id);
        }

        [Fact]
        public async Task GetClassRoomByIdAsync_StudentNotEnrolled_ThrowsForbiddenException()
        {
            var classRoom = AddClassRoom();
            _currentUser.As(Student, UserRole.Student);

            await Assert.ThrowsAsync<ForbiddenException>(() => _context.GetClassRoomByIdAsync(classRoom.Id));
        }

        [Fact]
        public async Task GetClassRoomByIdAsync_UnknownId_ThrowsNotFoundException()
        {
            _currentUser.As(Teacher, UserRole.Teacher);

            await Assert.ThrowsAsync<NotFoundException>(() => _context.GetClassRoomByIdAsync(Guid.NewGuid()));
        }

        [Fact]
        public async Task GetByTeacherWalletAsync_AnotherTeachersWallet_ThrowsForbiddenException()
        {
            AddClassRoom();
            _currentUser.As(OtherTeacher, UserRole.Teacher);

            await Assert.ThrowsAsync<ForbiddenException>(() => _context.GetByTeacherWalletAsync(Teacher));
        }

        [Fact]
        public async Task GetByTeacherWalletAsync_OwnWalletWithDifferentCase_ReturnsClassRooms()
        {
            AddClassRoom();
            _currentUser.As(Teacher, UserRole.Teacher);

            var classRooms = await _context.GetByTeacherWalletAsync("0xTeacher");

            Assert.Single(classRooms);
        }

        [Fact]
        public async Task DeleteClassRoomAsync_Owner_RemovesClassRoom()
        {
            var classRoom = AddClassRoom();
            _currentUser.As(Teacher, UserRole.Teacher);

            await _context.DeleteClassRoomAsync(classRoom.Id);

            Assert.Empty(_gateway.ClassRooms);
        }

        [Fact]
        public async Task DeleteClassRoomAsync_AnotherTeacher_ThrowsForbiddenException()
        {
            var classRoom = AddClassRoom();
            _currentUser.As(OtherTeacher, UserRole.Teacher);

            await Assert.ThrowsAsync<ForbiddenException>(() => _context.DeleteClassRoomAsync(classRoom.Id));
            Assert.Single(_gateway.ClassRooms);
        }

        [Fact]
        public async Task DeleteClassRoomAsync_Admin_RemovesAnyClassRoom()
        {
            var classRoom = AddClassRoom();
            _currentUser.As("0xadmin", UserRole.Admin);

            await _context.DeleteClassRoomAsync(classRoom.Id);

            Assert.Empty(_gateway.ClassRooms);
        }

        [Fact]
        public async Task DeleteClassRoomAsync_UnknownId_ThrowsNotFoundException()
        {
            _currentUser.As(Teacher, UserRole.Teacher);

            await Assert.ThrowsAsync<NotFoundException>(() => _context.DeleteClassRoomAsync(Guid.NewGuid()));
        }
    }
}
