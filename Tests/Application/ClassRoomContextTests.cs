using MathChain.Application.Contexts;
using MathChain.Application.DTOs;
using MathChain.Domain.Entities;
using MathChain.Domain.Exceptions;
using MathChain.Tests.Fakes;

namespace MathChain.Tests.Application
{
    public class ClassRoomContextTests
    {
        private const string AllowedJoinCodeChars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

        private readonly InMemoryClassRoomGateway _gateway = new();
        private readonly ClassRoomContext _context;

        public ClassRoomContextTests()
        {
            _context = new ClassRoomContext(_gateway);
        }

        private ClassRoom AddClassRoom(string joinCode = "ABC234")
        {
            var classRoom = new ClassRoom
            {
                Id = Guid.NewGuid(),
                Name = "Algebra",
                TeacherWallet = "0xteacher",
                JoinCode = joinCode,
                CreatedAt = DateTime.UtcNow
            };
            _gateway.ClassRooms.Add(classRoom);
            return classRoom;
        }

        [Fact]
        public async Task CreateClassRoomAsync_ValidRequest_ReturnsClassRoomWithValidJoinCode()
        {
            var request = new CreateClassRoomRequest { Name = "  Algebra  ", TeacherWallet = "0xTEACHER" };

            var classRoom = await _context.CreateClassRoomAsync(request);

            Assert.Equal("Algebra", classRoom.Name);
            Assert.Equal("0xteacher", classRoom.TeacherWallet);
            Assert.Equal(6, classRoom.JoinCode.Length);
            Assert.All(classRoom.JoinCode, c => Assert.Contains(c, AllowedJoinCodeChars));
        }

        [Fact]
        public async Task CreateClassRoomAsync_MissingName_ThrowsBusinessRuleException()
        {
            var request = new CreateClassRoomRequest { Name = "", TeacherWallet = "0xteacher" };

            await Assert.ThrowsAsync<BusinessRuleException>(() => _context.CreateClassRoomAsync(request));
            Assert.Empty(_gateway.ClassRooms);
        }

        [Fact]
        public async Task JoinClassRoomAsync_ValidCodeInLowerCase_EnrollsStudent()
        {
            var classRoom = AddClassRoom("ABC234");

            var joined = await _context.JoinClassRoomAsync(new JoinClassRoomRequest { JoinCode = " abc234 ", StudentWallet = "0xSTUDENT" });

            Assert.Equal(classRoom.Id, joined.Id);
            var enrollment = Assert.Single(_gateway.Enrollments);
            Assert.Equal("0xstudent", enrollment.StudentWallet);
        }

        [Fact]
        public async Task JoinClassRoomAsync_UnknownCode_ThrowsNotFoundException()
        {
            AddClassRoom("ABC234");

            var request = new JoinClassRoomRequest { JoinCode = "ZZZZZZ", StudentWallet = "0xstudent" };

            await Assert.ThrowsAsync<NotFoundException>(() => _context.JoinClassRoomAsync(request));
            Assert.Empty(_gateway.Enrollments);
        }

        [Fact]
        public async Task JoinClassRoomAsync_AlreadyEnrolled_ThrowsBusinessRuleException()
        {
            AddClassRoom("ABC234");
            var request = new JoinClassRoomRequest { JoinCode = "ABC234", StudentWallet = "0xstudent" };
            await _context.JoinClassRoomAsync(request);

            await Assert.ThrowsAsync<BusinessRuleException>(() => _context.JoinClassRoomAsync(request));
            Assert.Single(_gateway.Enrollments);
        }

        [Fact]
        public async Task JoinClassRoomAsync_MissingJoinCode_ThrowsBusinessRuleException()
        {
            var request = new JoinClassRoomRequest { JoinCode = "", StudentWallet = "0xstudent" };

            await Assert.ThrowsAsync<BusinessRuleException>(() => _context.JoinClassRoomAsync(request));
        }

        [Fact]
        public async Task GetClassRoomByIdAsync_UnknownId_ThrowsNotFoundException()
        {
            await Assert.ThrowsAsync<NotFoundException>(() => _context.GetClassRoomByIdAsync(Guid.NewGuid()));
        }

        [Fact]
        public async Task DeleteClassRoomAsync_ExistingId_RemovesClassRoom()
        {
            var classRoom = AddClassRoom();

            await _context.DeleteClassRoomAsync(classRoom.Id);

            Assert.Empty(_gateway.ClassRooms);
        }

        [Fact]
        public async Task DeleteClassRoomAsync_UnknownId_ThrowsNotFoundException()
        {
            await Assert.ThrowsAsync<NotFoundException>(() => _context.DeleteClassRoomAsync(Guid.NewGuid()));
        }
    }
}
