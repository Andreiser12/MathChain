using MathChain.Application.Contexts;
using MathChain.Domain.Entities;
using MathChain.Tests.Fakes;

namespace MathChain.Tests.Application
{
    public class ClassStatisticsContextTests
    {
        private readonly Guid _classRoomId = Guid.NewGuid();
        private readonly InMemoryClassRoomGateway _classRooms = new();
        private readonly InMemoryAssignmentGateway _assignments = new();
        private readonly InMemorySubmissionGateway _submissions = new();
        private readonly InMemoryGradeGateway _grades = new();
        private readonly ClassStatisticsContext _context;

        public ClassStatisticsContextTests()
        {
            _context = new ClassStatisticsContext(_classRooms, _assignments, _submissions, _grades);
        }

        private void Enroll(string wallet)
        {
            _classRooms.Enrollments.Add(new Enrollment { Id = Guid.NewGuid(), ClassRoomId = _classRoomId, StudentWallet = wallet });
        }

        private Assignment AddAssignment(DateTime? dueTime)
        {
            var assignment = new Assignment { Id = Guid.NewGuid(), ClassRoomId = _classRoomId, DueTime = dueTime };
            _assignments.Assignments.Add(assignment);
            return assignment;
        }

        private void Submit(Assignment assignment, string wallet, DateTime submittedAt, double score)
        {
            var submission = new Submission { Id = Guid.NewGuid(), AssignmentId = assignment.Id, StudentWallet = wallet, SubmittedAt = submittedAt };
            _submissions.Submissions.Add(submission);
            _grades.Grades.Add(new Grade { Id = Guid.NewGuid(), SubmissionId = submission.Id, Score = score, GradedAt = submittedAt });
        }

        [Fact]
        public async Task GetClassStatisticsAsync_MixedSubmissions_CountsOnTimeLateAndMissing()
        {
            Enroll("0xa");
            Enroll("0xb");
            Enroll("0xc");
            var homework1 = AddAssignment(new DateTime(2026, 1, 10, 12, 0, 0));
            var homework2 = AddAssignment(new DateTime(2026, 1, 20, 12, 0, 0));
            Submit(homework1, "0xa", new DateTime(2026, 1, 9), 9.5);
            Submit(homework1, "0xb", new DateTime(2026, 1, 10, 18, 0, 0), 4);

            var stats = await _context.GetClassStatisticsAsync(_classRoomId);

            Assert.Equal(3, stats.TotalEnrolledStudents);
            Assert.Equal(2, stats.TotalAssignments);
            Assert.Equal(1, stats.SubmissionsOnTime);
            Assert.Equal(1, stats.SubmissionsLate);
            Assert.Equal(4, stats.SubmissionsMissing);
            Assert.Equal(33.3, stats.SubmissionRate);
            Assert.Equal(6.75, stats.ClassAverage);
            Assert.Equal(9.5, stats.HighestGrade);
        }

        [Fact]
        public async Task GetClassStatisticsAsync_TestsInClass_AreNotCountedAsAssignments()
        {
            Enroll("0xa");
            AddAssignment(null);
            _assignments.Assignments.Add(new Test { Id = Guid.NewGuid(), ClassRoomId = _classRoomId });

            var stats = await _context.GetClassStatisticsAsync(_classRoomId);

            Assert.Equal(1, stats.TotalAssignments);
        }

        [Fact]
        public async Task GetClassStatisticsAsync_UnknownClassRoom_ReturnsEmptyStatistics()
        {
            var stats = await _context.GetClassStatisticsAsync(Guid.NewGuid());

            Assert.Equal(0, stats.TotalEnrolledStudents);
            Assert.Equal(0, stats.SubmissionRate);
            Assert.Empty(stats.WeeklyLabels);
        }
    }
}
