using MathChain.Application.Contexts;
using MathChain.Application.DTOs;
using MathChain.Domain.Entities;
using MathChain.Domain.Enums;
using MathChain.Domain.Exceptions;
using MathChain.Tests.Fakes;

namespace MathChain.Tests.Application
{
    public class AssignmentContextTests
    {
        private const string Teacher = "0xteacher";
        private const string OtherTeacher = "0xotherteacher";
        private const string Student = "0xstudent";
        private const string OtherStudent = "0xotherstudent";
        private const string Outsider = "0xoutsider";

        private readonly InMemoryClassRoomGateway _classRooms = new();
        private readonly InMemoryAssignmentGateway _assignments = new();
        private readonly InMemorySubmissionGateway _submissions = new();
        private readonly InMemoryGradeGateway _grades = new();
        private readonly FakeCurrentUser _currentUser = new();
        private readonly AssignmentContext _context;
        private readonly ClassRoom _classRoom;
        private readonly Assignment _assignment;

        public AssignmentContextTests()
        {
            _context = new AssignmentContext(_assignments, _submissions, _grades, new AccessGuard(_currentUser, _classRooms));

            _classRoom = new ClassRoom { Id = Guid.NewGuid(), Name = "Algebra", TeacherWallet = Teacher, JoinCode = "ABC234" };
            _classRooms.ClassRooms.Add(_classRoom);
            _classRooms.Enrollments.Add(new Enrollment { Id = Guid.NewGuid(), ClassRoomId = _classRoom.Id, StudentWallet = Student });
            _classRooms.Enrollments.Add(new Enrollment { Id = Guid.NewGuid(), ClassRoomId = _classRoom.Id, StudentWallet = OtherStudent });

            _assignment = new Assignment { Id = Guid.NewGuid(), ClassRoomId = _classRoom.Id, Title = "Tema 1", CreatedAt = DateTime.UtcNow };
            _assignments.Assignments.Add(_assignment);
        }

        private Submission AddSubmission(string studentWallet, string content = "answer")
        {
            var submission = new Submission { Id = Guid.NewGuid(), AssignmentId = _assignment.Id, StudentWallet = studentWallet, Content = content, SubmittedAt = DateTime.UtcNow };
            _submissions.Submissions.Add(submission);
            return submission;
        }

        [Fact]
        public async Task GetAssignmentsByClassRoomAsync_Member_ReturnsAssignmentsWithSubmissionCount()
        {
            AddSubmission(Student);
            AddSubmission(OtherStudent);
            _currentUser.As(Student, UserRole.Student);

            var assignments = await _context.GetAssignmentsByClassRoomAsync(_classRoom.Id);

            var assignment = Assert.Single(assignments);
            Assert.Equal("Tema 1", assignment.Title);
            Assert.Equal(2, assignment.SubmissionCount);
        }

        [Fact]
        public async Task GetAssignmentsByClassRoomAsync_Outsider_ThrowsForbiddenException()
        {
            _currentUser.As(Outsider, UserRole.Student);

            await Assert.ThrowsAsync<ForbiddenException>(() => _context.GetAssignmentsByClassRoomAsync(_classRoom.Id));
        }

        [Fact]
        public async Task GetAssignmentByIdAsync_UnknownAssignment_ThrowsNotFoundException()
        {
            _currentUser.As(Teacher, UserRole.Teacher);

            await Assert.ThrowsAsync<NotFoundException>(() => _context.GetAssignmentByIdAsync(Guid.NewGuid()));
        }

        [Fact]
        public async Task GetAssignmentByIdAsync_Outsider_ThrowsForbiddenException()
        {
            _currentUser.As(Outsider, UserRole.Student);

            await Assert.ThrowsAsync<ForbiddenException>(() => _context.GetAssignmentByIdAsync(_assignment.Id));
        }

        [Fact]
        public async Task CreateAssignmentAsync_Owner_StoresTrimmedAssignmentInClassRoom()
        {
            _currentUser.As(Teacher, UserRole.Teacher);

            var created = await _context.CreateAssignmentAsync(_classRoom.Id, new CreateAssignmentRequest { Title = "  Tema 2  ", Description = "  Integrale  ", ClassRoomId = Guid.NewGuid() });

            Assert.Equal("Tema 2", created.Title);
            Assert.Equal("Integrale", created.Description);
            Assert.Equal(_classRoom.Id, created.ClassRoomId);
            Assert.Contains(created, _assignments.Assignments);
        }

        [Fact]
        public async Task CreateAssignmentAsync_EmptyTitle_ThrowsBusinessRuleException()
        {
            _currentUser.As(Teacher, UserRole.Teacher);

            await Assert.ThrowsAsync<BusinessRuleException>(() => _context.CreateAssignmentAsync(_classRoom.Id, new CreateAssignmentRequest { Title = "   " }));
            Assert.Single(_assignments.Assignments);
        }

        [Theory]
        [InlineData(Student, UserRole.Student)]
        [InlineData(OtherTeacher, UserRole.Teacher)]
        public async Task CreateAssignmentAsync_NotOwner_ThrowsForbiddenAndStoresNothing(string wallet, UserRole role)
        {
            _currentUser.As(wallet, role);

            await Assert.ThrowsAsync<ForbiddenException>(() => _context.CreateAssignmentAsync(_classRoom.Id, new CreateAssignmentRequest { Title = "Hack" }));
            Assert.Single(_assignments.Assignments);
        }

        [Fact]
        public async Task DeleteAssignmentAsync_Owner_RemovesAssignment()
        {
            _currentUser.As(Teacher, UserRole.Teacher);

            await _context.DeleteAssignmentAsync(_assignment.Id);

            Assert.Empty(_assignments.Assignments);
        }

        [Fact]
        public async Task DeleteAssignmentAsync_Student_ThrowsForbiddenAndKeepsAssignment()
        {
            _currentUser.As(Student, UserRole.Student);

            await Assert.ThrowsAsync<ForbiddenException>(() => _context.DeleteAssignmentAsync(_assignment.Id));
            Assert.Single(_assignments.Assignments);
        }

        [Fact]
        public async Task DeleteAssignmentAsync_UnknownAssignment_ThrowsNotFoundException()
        {
            _currentUser.As(Teacher, UserRole.Teacher);

            await Assert.ThrowsAsync<NotFoundException>(() => _context.DeleteAssignmentAsync(Guid.NewGuid()));
        }

        [Fact]
        public async Task GetSubmissionsAsync_Owner_SeesEverySubmissionWithGrade()
        {
            var graded = AddSubmission(Student);
            AddSubmission(OtherStudent);
            _grades.Grades.Add(new Grade { Id = Guid.NewGuid(), SubmissionId = graded.Id, Score = 90, Feedback = "Bine" });
            _currentUser.As(Teacher, UserRole.Teacher);

            var submissions = await _context.GetSubmissionsAsync(_assignment.Id);

            Assert.Equal(2, submissions.Count);
            var gradedDto = Assert.Single(submissions, s => s.Id == graded.Id);
            Assert.Equal(90, gradedDto.Score);
            Assert.Equal("Bine", gradedDto.Feedback);
            Assert.Null(Assert.Single(submissions, s => s.Id != graded.Id).Score);
        }

        [Fact]
        public async Task GetSubmissionsAsync_Student_SeesOnlyOwnSubmission()
        {
            var own = AddSubmission(Student);
            AddSubmission(OtherStudent);
            _currentUser.As("0xSTUDENT", UserRole.Student);

            var submissions = await _context.GetSubmissionsAsync(_assignment.Id);

            Assert.Equal(own.Id, Assert.Single(submissions).Id);
        }

        [Fact]
        public async Task GetSubmissionsAsync_UnknownAssignment_ThrowsNotFoundException()
        {
            _currentUser.As(Teacher, UserRole.Teacher);

            await Assert.ThrowsAsync<NotFoundException>(() => _context.GetSubmissionsAsync(Guid.NewGuid()));
        }

        [Fact]
        public async Task SubmitAssignmentAsync_EnrolledStudent_UsesWalletFromToken()
        {
            _currentUser.As("0xStudent", UserRole.Student);

            var submission = await _context.SubmitAssignmentAsync(_assignment.Id, new SubmitAssignmentRequest { StudentWallet = OtherStudent, Content = "x = 2" });

            Assert.Equal(Student, submission.StudentWallet);
            Assert.Equal("x = 2", Assert.Single(_submissions.Submissions).Content);
        }

        [Fact]
        public async Task SubmitAssignmentAsync_SecondSubmission_UpdatesExistingSubmission()
        {
            var first = AddSubmission(Student, "first");
            _currentUser.As(Student, UserRole.Student);

            var second = await _context.SubmitAssignmentAsync(_assignment.Id, new SubmitAssignmentRequest { Content = "second", FileName = "tema.pdf" });

            var stored = Assert.Single(_submissions.Submissions);
            Assert.Equal(first.Id, second.Id);
            Assert.Equal("second", stored.Content);
            Assert.Equal("tema.pdf", stored.FileName);
        }

        [Theory]
        [InlineData(Teacher, UserRole.Teacher)]
        [InlineData(Outsider, UserRole.Student)]
        public async Task SubmitAssignmentAsync_NotEnrolledStudent_ThrowsForbiddenException(string wallet, UserRole role)
        {
            _currentUser.As(wallet, role);

            await Assert.ThrowsAsync<ForbiddenException>(() => _context.SubmitAssignmentAsync(_assignment.Id, new SubmitAssignmentRequest { Content = "x" }));
            Assert.Empty(_submissions.Submissions);
        }

        [Fact]
        public async Task SubmitAssignmentAsync_UnknownAssignment_ThrowsNotFoundException()
        {
            _currentUser.As(Student, UserRole.Student);

            await Assert.ThrowsAsync<NotFoundException>(() => _context.SubmitAssignmentAsync(Guid.NewGuid(), new SubmitAssignmentRequest()));
        }

        [Fact]
        public async Task GradeSubmissionAsync_Owner_SavesGrade()
        {
            var submission = AddSubmission(Student);
            _currentUser.As(Teacher, UserRole.Teacher);

            var grade = await _context.GradeSubmissionAsync(submission.Id, new GradeSubmissionRequest { Score = 85, Feedback = null });

            Assert.Equal(submission.Id, grade.SubmissionId);
            Assert.Equal(85, grade.Score);
            Assert.Equal(string.Empty, grade.Feedback);
            Assert.Single(_grades.Grades);
        }

        [Fact]
        public async Task GradeSubmissionAsync_Regrade_UpdatesExistingGrade()
        {
            var submission = AddSubmission(Student);
            _grades.Grades.Add(new Grade { Id = Guid.NewGuid(), SubmissionId = submission.Id, Score = 50, Feedback = "Slab" });
            _currentUser.As(Teacher, UserRole.Teacher);

            await _context.GradeSubmissionAsync(submission.Id, new GradeSubmissionRequest { Score = 95, Feedback = "Corectat" });

            var stored = Assert.Single(_grades.Grades);
            Assert.Equal(95, stored.Score);
            Assert.Equal("Corectat", stored.Feedback);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(100.5)]
        public async Task GradeSubmissionAsync_ScoreOutsideRange_ThrowsBusinessRuleException(double score)
        {
            var submission = AddSubmission(Student);
            _currentUser.As(Teacher, UserRole.Teacher);

            await Assert.ThrowsAsync<BusinessRuleException>(() => _context.GradeSubmissionAsync(submission.Id, new GradeSubmissionRequest { Score = score }));
            Assert.Empty(_grades.Grades);
        }

        [Theory]
        [InlineData(Student, UserRole.Student)]
        [InlineData(OtherTeacher, UserRole.Teacher)]
        public async Task GradeSubmissionAsync_NotOwner_ThrowsForbiddenException(string wallet, UserRole role)
        {
            var submission = AddSubmission(Student);
            _currentUser.As(wallet, role);

            await Assert.ThrowsAsync<ForbiddenException>(() => _context.GradeSubmissionAsync(submission.Id, new GradeSubmissionRequest { Score = 100 }));
            Assert.Empty(_grades.Grades);
        }

        [Fact]
        public async Task GradeSubmissionAsync_UnknownSubmission_ThrowsNotFoundException()
        {
            _currentUser.As(Teacher, UserRole.Teacher);

            await Assert.ThrowsAsync<NotFoundException>(() => _context.GradeSubmissionAsync(Guid.NewGuid(), new GradeSubmissionRequest { Score = 10 }));
        }
    }
}
