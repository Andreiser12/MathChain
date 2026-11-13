using MathChain.Application.Contexts;
using MathChain.Domain.Entities;
using MathChain.Domain.Enums;
using MathChain.Domain.Exceptions;
using MathChain.Tests.Fakes;

namespace MathChain.Tests.Application
{
    public class GradebookContextTests
    {
        private const string StudentWallet = "0xstudent000000000001";

        private readonly Guid _classRoomId = Guid.NewGuid();
        private readonly InMemoryClassRoomGateway _classRooms = new();
        private readonly InMemoryAssignmentGateway _assignments = new();
        private readonly InMemorySubmissionGateway _submissions = new();
        private readonly InMemoryGradeGateway _grades = new();
        private readonly FakeCurrentUser _currentUser = new();
        private readonly GradebookContext _context;

        public GradebookContextTests()
        {
            _context = new GradebookContext(_classRooms, _assignments, _submissions, _grades, new AccessGuard(_currentUser, _classRooms));
            _classRooms.ClassRooms.Add(new ClassRoom { Id = _classRoomId, TeacherWallet = "0xteacher" });
            _currentUser.As("0xteacher", UserRole.Teacher);
            _classRooms.Enrollments.Add(new Enrollment { Id = Guid.NewGuid(), ClassRoomId = _classRoomId, StudentWallet = StudentWallet });
        }

        private void AddGradedWork(AssignmentBase work, double score, DateTime gradedAt)
        {
            work.ClassRoomId = _classRoomId;
            _assignments.Assignments.Add(work);

            var submission = new Submission { Id = Guid.NewGuid(), AssignmentId = work.Id, StudentWallet = StudentWallet };
            _submissions.Submissions.Add(submission);
            _grades.Grades.Add(new Grade { Id = Guid.NewGuid(), SubmissionId = submission.Id, Score = score, GradedAt = gradedAt });
        }

        [Fact]
        public async Task GetGradebookAsync_HomeworkAndTestGrades_ReturnsAllGradesOrderedByDate()
        {
            AddGradedWork(new Test { Id = Guid.NewGuid() }, 8, new DateTime(2026, 2, 1));
            AddGradedWork(new Assignment { Id = Guid.NewGuid() }, 9.5, new DateTime(2026, 1, 10));

            var gradebook = await _context.GetGradebookAsync(_classRoomId);

            var entry = Assert.Single(gradebook);
            Assert.Equal("0xstud...0001", entry.StudentName);
            Assert.Equal(new[] { 9.5, 8.0 }, entry.Grades.Select(g => g.Score));
        }

        [Fact]
        public async Task SetFinalGradeAsync_EnrolledStudent_UpdatesFinalGrade()
        {
            await _context.SetFinalGradeAsync(_classRoomId, StudentWallet, 9);

            Assert.Equal(9, _classRooms.Enrollments.Single().FinalGrade);
        }

        [Fact]
        public async Task SetFinalGradeAsync_UnknownStudent_ThrowsNotFoundException()
        {
            await Assert.ThrowsAsync<NotFoundException>(() => _context.SetFinalGradeAsync(_classRoomId, "0xnobody", 9));
        }

        [Fact]
        public async Task RemoveStudentAsync_EnrolledStudent_RemovesEnrollment()
        {
            await _context.RemoveStudentAsync(_classRoomId, StudentWallet);

            Assert.Empty(_classRooms.Enrollments);
        }

        [Fact]
        public async Task GetGradebookAsync_AnotherTeacher_ThrowsForbiddenException()
        {
            _currentUser.As("0xotherteacher", UserRole.Teacher);

            await Assert.ThrowsAsync<ForbiddenException>(() => _context.GetGradebookAsync(_classRoomId));
        }

        [Fact]
        public async Task SetFinalGradeAsync_EnrolledStudent_ThrowsForbiddenException()
        {
            _currentUser.As(StudentWallet, UserRole.Student);

            await Assert.ThrowsAsync<ForbiddenException>(() => _context.SetFinalGradeAsync(_classRoomId, StudentWallet, 10));
            Assert.Null(_classRooms.Enrollments.Single().FinalGrade);
        }

        [Fact]
        public async Task RemoveStudentAsync_UnknownStudent_ThrowsNotFoundException()
        {
            await Assert.ThrowsAsync<NotFoundException>(() => _context.RemoveStudentAsync(_classRoomId, "0xnobody"));
        }
    }
}
