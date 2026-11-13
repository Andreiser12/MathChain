using MathChain.Application.Contexts;
using MathChain.Domain.Entities;
using MathChain.Domain.Enums;
using MathChain.Domain.Exceptions;
using MathChain.Tests.Fakes;

namespace MathChain.Tests.Application
{
    public class AccessGuardTests
    {
        private const string Teacher = "0xteacher";
        private const string Student = "0xstudent";
        private const string Outsider = "0xoutsider";
        private const string Admin = "0xadmin";

        private readonly InMemoryClassRoomGateway _gateway = new();
        private readonly FakeCurrentUser _currentUser = new();
        private readonly AccessGuard _guard;
        private readonly ClassRoom _classRoom;

        public AccessGuardTests()
        {
            _guard = new AccessGuard(_currentUser, _gateway);
            _classRoom = new ClassRoom { Id = Guid.NewGuid(), Name = "Algebra", TeacherWallet = Teacher, JoinCode = "ABC234" };
            _gateway.ClassRooms.Add(_classRoom);
            _gateway.Enrollments.Add(new Enrollment { Id = Guid.NewGuid(), ClassRoomId = _classRoom.Id, StudentWallet = Student });
        }

        [Fact]
        public void RequireWallet_Anonymous_ThrowsUnauthorizedException()
        {
            _currentUser.Anonymous();

            Assert.Throws<UnauthorizedException>(() => _guard.RequireWallet());
        }

        [Fact]
        public void RequireWallet_ReturnsTrimmedLowercaseWallet()
        {
            _currentUser.As("  0xABCdef  ", UserRole.Student);

            Assert.Equal("0xabcdef", _guard.RequireWallet());
        }

        [Fact]
        public void RequireRole_AllowedRole_ReturnsWallet()
        {
            _currentUser.As(Teacher, UserRole.Teacher);

            Assert.Equal(Teacher, _guard.RequireRole(UserRole.Teacher));
        }

        [Fact]
        public void RequireRole_OtherRole_ThrowsForbiddenException()
        {
            _currentUser.As(Student, UserRole.Student);

            Assert.Throws<ForbiddenException>(() => _guard.RequireRole(UserRole.Teacher));
        }

        [Fact]
        public void RequireRole_Admin_PassesAnyRoleCheck()
        {
            _currentUser.As(Admin, UserRole.Admin);

            Assert.Equal(Admin, _guard.RequireRole(UserRole.Student));
        }

        [Fact]
        public void RequireRole_Anonymous_ThrowsUnauthorizedBeforeForbidden()
        {
            _currentUser.Anonymous();

            Assert.Throws<UnauthorizedException>(() => _guard.RequireRole(UserRole.Teacher));
        }

        [Fact]
        public void RequireSelfOrAdmin_SameWalletDifferentCase_IsAllowed()
        {
            _currentUser.As(Student, UserRole.Student);

            Assert.Equal(Student, _guard.RequireSelfOrAdmin(" 0xSTUDENT "));
        }

        [Fact]
        public void RequireSelfOrAdmin_OtherWallet_ThrowsForbiddenException()
        {
            _currentUser.As(Student, UserRole.Student);

            Assert.Throws<ForbiddenException>(() => _guard.RequireSelfOrAdmin(Outsider));
        }

        [Fact]
        public void RequireSelfOrAdmin_Admin_CanAccessAnyWallet()
        {
            _currentUser.As(Admin, UserRole.Admin);

            Assert.Equal(Admin, _guard.RequireSelfOrAdmin(Outsider));
        }

        [Fact]
        public async Task RequireOwnerAsync_Owner_ReturnsClassRoom()
        {
            _currentUser.As("0xTeacher", UserRole.Teacher);

            var classRoom = await _guard.RequireOwnerAsync(_classRoom.Id);

            Assert.Equal(_classRoom.Id, classRoom.Id);
        }

        [Fact]
        public async Task RequireOwnerAsync_EnrolledStudent_ThrowsForbiddenException()
        {
            _currentUser.As(Student, UserRole.Student);

            await Assert.ThrowsAsync<ForbiddenException>(() => _guard.RequireOwnerAsync(_classRoom.Id));
        }

        [Fact]
        public async Task RequireOwnerAsync_UnknownClassRoom_ThrowsNotFoundException()
        {
            _currentUser.As(Teacher, UserRole.Teacher);

            await Assert.ThrowsAsync<NotFoundException>(() => _guard.RequireOwnerAsync(Guid.NewGuid()));
        }

        [Fact]
        public async Task RequireOwnerAsync_Anonymous_ThrowsUnauthorizedBeforeLookup()
        {
            _currentUser.Anonymous();

            await Assert.ThrowsAsync<UnauthorizedException>(() => _guard.RequireOwnerAsync(Guid.NewGuid()));
        }

        [Theory]
        [InlineData(Teacher, UserRole.Teacher)]
        [InlineData("0xSTUDENT", UserRole.Student)]
        [InlineData(Admin, UserRole.Admin)]
        public async Task RequireMemberAsync_OwnerEnrolledStudentOrAdmin_IsAllowed(string wallet, UserRole role)
        {
            _currentUser.As(wallet, role);

            var classRoom = await _guard.RequireMemberAsync(_classRoom.Id);

            Assert.Equal(_classRoom.Id, classRoom.Id);
        }

        [Fact]
        public async Task RequireMemberAsync_Outsider_ThrowsForbiddenException()
        {
            _currentUser.As(Outsider, UserRole.Student);

            await Assert.ThrowsAsync<ForbiddenException>(() => _guard.RequireMemberAsync(_classRoom.Id));
        }

        [Fact]
        public async Task RequireEnrolledStudentAsync_EnrolledStudent_ReturnsWallet()
        {
            _currentUser.As("0xStudent", UserRole.Student);

            Assert.Equal(Student, await _guard.RequireEnrolledStudentAsync(_classRoom.Id));
        }

        [Fact]
        public async Task RequireEnrolledStudentAsync_OwnerTeacher_ThrowsForbiddenException()
        {
            _currentUser.As(Teacher, UserRole.Teacher);

            var exception = await Assert.ThrowsAsync<ForbiddenException>(() => _guard.RequireEnrolledStudentAsync(_classRoom.Id));
            Assert.Equal("Only students enrolled in this classroom can do this.", exception.Message);
        }
    }
}
