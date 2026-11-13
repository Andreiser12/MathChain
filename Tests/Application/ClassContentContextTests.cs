using MathChain.Application.Contexts;
using MathChain.Application.DTOs;
using MathChain.Domain.Entities;
using MathChain.Domain.Enums;
using MathChain.Domain.Exceptions;
using MathChain.Tests.Fakes;

namespace MathChain.Tests.Application
{
    public class ClassContentContextTests
    {
        private const string Teacher = "0xteacher00000000000000000000000000000001";
        private const string Student = "0xstudent00000000000000000000000000000002";
        private const string OtherStudent = "0xotherstudent000000000000000000000000003";
        private const string Outsider = "0xoutsider0000000000000000000000000000004";

        private readonly InMemoryClassRoomGateway _classRooms = new();
        private readonly InMemoryClassContentGateway _content = new();
        private readonly FakeCurrentUser _currentUser = new();
        private readonly ClassContentContext _context;
        private readonly ClassRoom _classRoom;

        public ClassContentContextTests()
        {
            _context = new ClassContentContext(_content, new AccessGuard(_currentUser, _classRooms));

            _classRoom = new ClassRoom { Id = Guid.NewGuid(), Name = "Algebra", TeacherWallet = Teacher, JoinCode = "ABC234" };
            _classRooms.ClassRooms.Add(_classRoom);
            _classRooms.Enrollments.Add(new Enrollment { Id = Guid.NewGuid(), ClassRoomId = _classRoom.Id, StudentWallet = Student });
            _classRooms.Enrollments.Add(new Enrollment { Id = Guid.NewGuid(), ClassRoomId = _classRoom.Id, StudentWallet = OtherStudent });
        }

        private ClassAnnouncement AddAnnouncement(string title, DateTime createdAt)
        {
            var announcement = new ClassAnnouncement { Id = Guid.NewGuid(), ClassRoomId = _classRoom.Id, Title = title, AuthorWallet = Teacher, CreatedAt = createdAt };
            _content.Announcements.Add(announcement);
            return announcement;
        }

        private ClassMedia AddMedia(string uploaderWallet)
        {
            var media = new ClassMedia { Id = Guid.NewGuid(), ClassRoomId = _classRoom.Id, FileName = "poza.png", IpfsHash = "QmMedia", UploaderWallet = uploaderWallet, CreatedAt = DateTime.UtcNow };
            _content.Media.Add(media);
            return media;
        }

        private ClassResource AddResource()
        {
            var resource = new ClassResource { Id = Guid.NewGuid(), ClassRoomId = _classRoom.Id, WeekNumber = 1, Title = "Fisa", IpfsHash = "QmFisa", CreatedAt = DateTime.UtcNow };
            _content.Resources.Add(resource);
            return resource;
        }

        [Fact]
        public async Task GetAnnouncementsAsync_Member_ReturnsNewestFirstWithShortAuthorName()
        {
            AddAnnouncement("Vechi", DateTime.UtcNow.AddDays(-1));
            AddAnnouncement("Nou", DateTime.UtcNow);
            _currentUser.As(Student, UserRole.Student);

            var announcements = await _context.GetAnnouncementsAsync(_classRoom.Id);

            Assert.Equal(new[] { "Nou", "Vechi" }, announcements.Select(a => a.Title));
            Assert.Equal("0xteac...0001", announcements[0].AuthorName);
        }

        [Fact]
        public async Task GetAnnouncementsAsync_ReturnsCommentsOfEachAnnouncement()
        {
            var announcement = AddAnnouncement("Anunt", DateTime.UtcNow);
            _content.Comments.Add(new MessageComment { Id = Guid.NewGuid(), MessageId = announcement.Id, AuthorWallet = Student, Content = "Mersi", CreatedAt = DateTime.UtcNow.AddMinutes(1) });
            _content.Comments.Add(new MessageComment { Id = Guid.NewGuid(), MessageId = announcement.Id, AuthorWallet = OtherStudent, Content = "Prima", CreatedAt = DateTime.UtcNow });
            _content.Comments.Add(new MessageComment { Id = Guid.NewGuid(), MessageId = Guid.NewGuid(), AuthorWallet = Student, Content = "Alt anunt", CreatedAt = DateTime.UtcNow });
            _currentUser.As(Student, UserRole.Student);

            var dto = Assert.Single(await _context.GetAnnouncementsAsync(_classRoom.Id));

            Assert.Equal(new[] { "Prima", "Mersi" }, dto.Comments.Select(c => c.Content));
            Assert.Equal("0xstud...0002", dto.Comments[1].AuthorName);
        }

        [Fact]
        public async Task GetAnnouncementsAsync_Outsider_ThrowsForbiddenException()
        {
            _currentUser.As(Outsider, UserRole.Student);

            await Assert.ThrowsAsync<ForbiddenException>(() => _context.GetAnnouncementsAsync(_classRoom.Id));
        }

        [Fact]
        public async Task CreateAnnouncementAsync_Owner_UsesWalletFromToken()
        {
            _currentUser.As(Teacher.ToUpperInvariant().Replace("0X", "0x"), UserRole.Teacher);

            var created = await _context.CreateAnnouncementAsync(_classRoom.Id, new CreateAnnouncementRequest { Title = "  Test luni  ", Content = "  Capitolul 3  ", AuthorWallet = Outsider });

            Assert.Equal("Test luni", created.Title);
            Assert.Equal("Capitolul 3", created.Content);
            Assert.Equal(Teacher, created.AuthorWallet);
            Assert.Single(_content.Announcements);
        }

        [Fact]
        public async Task CreateAnnouncementAsync_EmptyTitle_ThrowsBusinessRuleException()
        {
            _currentUser.As(Teacher, UserRole.Teacher);

            await Assert.ThrowsAsync<BusinessRuleException>(() => _context.CreateAnnouncementAsync(_classRoom.Id, new CreateAnnouncementRequest { Title = " " }));
        }

        [Fact]
        public async Task CreateAnnouncementAsync_Student_ThrowsForbiddenException()
        {
            _currentUser.As(Student, UserRole.Student);

            await Assert.ThrowsAsync<ForbiddenException>(() => _context.CreateAnnouncementAsync(_classRoom.Id, new CreateAnnouncementRequest { Title = "Hack" }));
            Assert.Empty(_content.Announcements);
        }

        [Fact]
        public async Task AddCommentAsync_EnrolledStudent_StoresTrimmedCommentFromToken()
        {
            var announcement = AddAnnouncement("Anunt", DateTime.UtcNow);
            _currentUser.As(Student, UserRole.Student);

            var comment = await _context.AddCommentAsync(announcement.Id, new AddCommentRequest { Content = "  Am inteles  ", AuthorWallet = Outsider });

            Assert.Equal("Am inteles", comment.Content);
            Assert.Equal(Student, comment.AuthorWallet);
            Assert.Equal(announcement.Id, comment.MessageId);
        }

        [Fact]
        public async Task AddCommentAsync_UnknownMessage_ThrowsNotFoundException()
        {
            _currentUser.As(Student, UserRole.Student);

            await Assert.ThrowsAsync<NotFoundException>(() => _context.AddCommentAsync(Guid.NewGuid(), new AddCommentRequest { Content = "x" }));
        }

        [Fact]
        public async Task AddCommentAsync_Outsider_ThrowsForbiddenException()
        {
            var announcement = AddAnnouncement("Anunt", DateTime.UtcNow);
            _currentUser.As(Outsider, UserRole.Student);

            await Assert.ThrowsAsync<ForbiddenException>(() => _context.AddCommentAsync(announcement.Id, new AddCommentRequest { Content = "spam" }));
            Assert.Empty(_content.Comments);
        }

        [Fact]
        public async Task AddCommentAsync_EmptyContent_ThrowsBusinessRuleException()
        {
            var announcement = AddAnnouncement("Anunt", DateTime.UtcNow);
            _currentUser.As(Student, UserRole.Student);

            await Assert.ThrowsAsync<BusinessRuleException>(() => _context.AddCommentAsync(announcement.Id, new AddCommentRequest { Content = "  " }));
        }

        [Fact]
        public async Task AddMediaAsync_EnrolledStudent_UsesWalletFromToken()
        {
            _currentUser.As(Student, UserRole.Student);

            var media = await _context.AddMediaAsync(_classRoom.Id, new CreateClassMediaRequest { FileName = " schema.png ", IpfsHash = " QmSchema ", UploaderWallet = Teacher });

            Assert.Equal(Student, media.UploaderWallet);
            Assert.Equal("schema.png", media.FileName);
            Assert.Equal("QmSchema", media.IpfsHash);
        }

        [Fact]
        public async Task AddMediaAsync_MissingIpfsHash_ThrowsBusinessRuleException()
        {
            _currentUser.As(Student, UserRole.Student);

            await Assert.ThrowsAsync<BusinessRuleException>(() => _context.AddMediaAsync(_classRoom.Id, new CreateClassMediaRequest { FileName = "a.png" }));
        }

        [Fact]
        public async Task AddMediaAsync_Outsider_ThrowsForbiddenException()
        {
            _currentUser.As(Outsider, UserRole.Student);

            await Assert.ThrowsAsync<ForbiddenException>(() => _context.AddMediaAsync(_classRoom.Id, new CreateClassMediaRequest { FileName = "a.png", IpfsHash = "Qm" }));
        }

        [Theory]
        [InlineData(Student, UserRole.Student)]
        [InlineData(Teacher, UserRole.Teacher)]
        public async Task DeleteMediaAsync_UploaderOrTeacher_RemovesMedia(string wallet, UserRole role)
        {
            var media = AddMedia(Student);
            _currentUser.As(wallet, role);

            await _context.DeleteMediaAsync(media.Id);

            Assert.DoesNotContain(_content.Media, m => m.Id == media.Id);
        }

        [Fact]
        public async Task DeleteMediaAsync_OtherStudent_ThrowsForbiddenAndKeepsMedia()
        {
            var media = AddMedia(Student);
            _currentUser.As(OtherStudent, UserRole.Student);

            await Assert.ThrowsAsync<ForbiddenException>(() => _context.DeleteMediaAsync(media.Id));
            Assert.Contains(_content.Media, m => m.Id == media.Id);
        }

        [Fact]
        public async Task DeleteMediaAsync_UnknownMedia_ThrowsNotFoundException()
        {
            _currentUser.As(Teacher, UserRole.Teacher);

            await Assert.ThrowsAsync<NotFoundException>(() => _context.DeleteMediaAsync(Guid.NewGuid()));
        }

        [Fact]
        public async Task GetResourcesAsync_EnrolledStudent_ReturnsResources()
        {
            AddResource();
            _currentUser.As(Student, UserRole.Student);

            Assert.Single(await _context.GetResourcesAsync(_classRoom.Id));
        }

        [Fact]
        public async Task AddResourceAsync_Owner_StoresTrimmedResource()
        {
            _currentUser.As(Teacher, UserRole.Teacher);

            var resource = await _context.AddResourceAsync(_classRoom.Id, new CreateResourceRequest { WeekNumber = 3, Title = " Fisa 3 ", IpfsHash = " QmFisa3 ", FileName = null! });

            Assert.Equal("Fisa 3", resource.Title);
            Assert.Equal("QmFisa3", resource.IpfsHash);
            Assert.Equal(string.Empty, resource.FileName);
            Assert.Equal(3, resource.WeekNumber);
            Assert.Equal(_classRoom.Id, resource.ClassRoomId);
        }

        [Fact]
        public async Task AddResourceAsync_Student_ThrowsForbiddenException()
        {
            _currentUser.As(Student, UserRole.Student);

            await Assert.ThrowsAsync<ForbiddenException>(() => _context.AddResourceAsync(_classRoom.Id, new CreateResourceRequest { Title = "Hack", IpfsHash = "Qm" }));
            Assert.Empty(_content.Resources);
        }

        [Fact]
        public async Task AddResourceAsync_MissingTitle_ThrowsBusinessRuleException()
        {
            _currentUser.As(Teacher, UserRole.Teacher);

            await Assert.ThrowsAsync<BusinessRuleException>(() => _context.AddResourceAsync(_classRoom.Id, new CreateResourceRequest { IpfsHash = "Qm" }));
        }

        [Fact]
        public async Task DeleteResourceAsync_Owner_RemovesResource()
        {
            var resource = AddResource();
            _currentUser.As(Teacher, UserRole.Teacher);

            await _context.DeleteResourceAsync(resource.Id);

            Assert.Empty(_content.Resources);
        }

        [Fact]
        public async Task DeleteResourceAsync_Student_ThrowsForbiddenAndKeepsResource()
        {
            AddResource();
            _currentUser.As(Student, UserRole.Student);

            await Assert.ThrowsAsync<ForbiddenException>(() => _context.DeleteResourceAsync(_content.Resources[0].Id));
            Assert.Single(_content.Resources);
        }

        [Fact]
        public async Task DeleteResourceAsync_UnknownResource_ThrowsNotFoundException()
        {
            _currentUser.As(Teacher, UserRole.Teacher);

            await Assert.ThrowsAsync<NotFoundException>(() => _context.DeleteResourceAsync(Guid.NewGuid()));
        }
    }
}
