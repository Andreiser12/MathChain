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
        public void RequireWallet_ShouldThrowUnauthorizedException_WhenTheUserIsAnonymous()
        {
            _currentUser.Anonymous();

            Assert.Throws<UnauthorizedException>(() => _guard.RequireWallet());
        }

        [Fact]
        public void RequireWallet_ShouldReturnATrimmedLowercaseWallet_WhenTheWalletHasSpacesAndUppercaseLetters()
        {
            _currentUser.As("  0xABCdef  ", UserRole.Student);

            Assert.Equal("0xabcdef", _guard.RequireWallet());
        }

        [Fact]
        public void RequireRole_ShouldReturnTheWallet_WhenTheRoleIsAllowed()
        {
            _currentUser.As(Teacher, UserRole.Teacher);

            Assert.Equal(Teacher, _guard.RequireRole(UserRole.Teacher));
        }

        [Fact]
        public void RequireRole_ShouldThrowForbiddenException_WhenTheRoleIsNotAllowed()
        {
            _currentUser.As(Student, UserRole.Student);

            Assert.Throws<ForbiddenException>(() => _guard.RequireRole(UserRole.Teacher));
        }

        [Fact]
        public void RequireRole_ShouldPassAnyRoleCheck_WhenTheUserIsAnAdmin()
        {
            _currentUser.As(Admin, UserRole.Admin);

            Assert.Equal(Admin, _guard.RequireRole(UserRole.Student));
        }

        [Fact]
        public void RequireRole_ShouldThrowUnauthorizedBeforeForbidden_WhenTheUserIsAnonymous()
        {
            _currentUser.Anonymous();

            Assert.Throws<UnauthorizedException>(() => _guard.RequireRole(UserRole.Teacher));
        }

        [Fact]
        public void RequireSelfOrAdmin_ShouldAllowAccess_WhenTheWalletIsTheSameWithADifferentCase()
        {
            _currentUser.As(Student, UserRole.Student);

            Assert.Equal(Student, _guard.RequireSelfOrAdmin(" 0xSTUDENT "));
        }

        [Fact]
        public void RequireSelfOrAdmin_ShouldThrowForbiddenException_WhenTheWalletBelongsToSomeoneElse()
        {
            _currentUser.As(Student, UserRole.Student);

            Assert.Throws<ForbiddenException>(() => _guard.RequireSelfOrAdmin(Outsider));
        }

        [Fact]
        public void RequireSelfOrAdmin_ShouldAllowAnyWallet_WhenTheUserIsAnAdmin()
        {
            _currentUser.As(Admin, UserRole.Admin);

            Assert.Equal(Admin, _guard.RequireSelfOrAdmin(Outsider));
        }

        [Fact]
        public async Task RequireOwnerAsync_ShouldReturnTheClassRoom_WhenTheUserOwnsIt()
        {
            _currentUser.As("0xTeacher", UserRole.Teacher);

            var classRoom = await _guard.RequireOwnerAsync(_classRoom.Id);

            Assert.Equal(_classRoom.Id, classRoom.Id);
        }

        [Fact]
        public async Task RequireOwnerAsync_ShouldThrowForbiddenException_WhenTheUserIsAnEnrolledStudent()
        {
            _currentUser.As(Student, UserRole.Student);

            await Assert.ThrowsAsync<ForbiddenException>(() => _guard.RequireOwnerAsync(_classRoom.Id));
        }

        [Fact]
        public async Task RequireOwnerAsync_ShouldThrowNotFoundException_WhenTheClassRoomDoesNotExist()
        {
            _currentUser.As(Teacher, UserRole.Teacher);

            await Assert.ThrowsAsync<NotFoundException>(() => _guard.RequireOwnerAsync(Guid.NewGuid()));
        }

        [Fact]
        public async Task RequireOwnerAsync_ShouldThrowUnauthorizedBeforeTheLookup_WhenTheUserIsAnonymous()
        {
            _currentUser.Anonymous();

            await Assert.ThrowsAsync<UnauthorizedException>(() => _guard.RequireOwnerAsync(Guid.NewGuid()));
        }

        [Theory]
        [InlineData(Teacher, UserRole.Teacher)]
        [InlineData("0xSTUDENT", UserRole.Student)]
        [InlineData(Admin, UserRole.Admin)]
        public async Task RequireMemberAsync_ShouldAllowAccess_WhenTheUserIsTheOwnerAnEnrolledStudentOrAnAdmin(string wallet, UserRole role)
        {
            _currentUser.As(wallet, role);

            var classRoom = await _guard.RequireMemberAsync(_classRoom.Id);

            Assert.Equal(_classRoom.Id, classRoom.Id);
        }

        [Fact]
        public async Task RequireMemberAsync_ShouldThrowForbiddenException_WhenTheUserIsNotAMember()
        {
            _currentUser.As(Outsider, UserRole.Student);

            await Assert.ThrowsAsync<ForbiddenException>(() => _guard.RequireMemberAsync(_classRoom.Id));
        }

        [Fact]
        public async Task RequireEnrolledStudentAsync_ShouldReturnTheWallet_WhenTheStudentIsEnrolled()
        {
            _currentUser.As("0xStudent", UserRole.Student);

            Assert.Equal(Student, await _guard.RequireEnrolledStudentAsync(_classRoom.Id));
        }

        [Fact]
        public async Task RequireEnrolledStudentAsync_ShouldThrowForbiddenException_WhenTheUserIsTheOwnerTeacher()
        {
            _currentUser.As(Teacher, UserRole.Teacher);

            var exception = await Assert.ThrowsAsync<ForbiddenException>(() => _guard.RequireEnrolledStudentAsync(_classRoom.Id));
            Assert.Equal("Only students enrolled in this classroom can do this.", exception.Message);
        }
    }
}
