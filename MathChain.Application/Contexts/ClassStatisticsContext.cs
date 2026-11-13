using MathChain.Application.DTOs;
using MathChain.Domain.Gateways;

namespace MathChain.Application.Contexts
{
    public class ClassStatisticsContext : IClassStatisticsContext
    {
        private const int MaxListedStudents = 5;
        private const int MaxWeeks = 6;

        private readonly IClassRoomGateway _classRoomGateway;
        private readonly IAssignmentGateway _assignmentGateway;
        private readonly ISubmissionGateway _submissionGateway;
        private readonly IGradeGateway _gradeGateway;
        private readonly AccessGuard _access;

        public ClassStatisticsContext(IClassRoomGateway classRoomGateway, IAssignmentGateway assignmentGateway,
            ISubmissionGateway submissionGateway, IGradeGateway gradeGateway, AccessGuard access)
        {
            _classRoomGateway = classRoomGateway;
            _assignmentGateway = assignmentGateway;
            _submissionGateway = submissionGateway;
            _gradeGateway = gradeGateway;
            _access = access;
        }

        public async Task<ClassStatisticsDto> GetClassStatisticsAsync(Guid classRoomId)
        {
            await _access.RequireOwnerAsync(classRoomId);

            var enrollments = await _classRoomGateway.GetEnrollmentsAsync(classRoomId);
            var assignments = await _assignmentGateway.GetByClassRoomIdAsync(classRoomId);
            var assignmentIds = assignments.Select(a => a.Id).ToList();
            var submissions = await _submissionGateway.GetByAssignmentIdsAsync(assignmentIds);
            var submissionIds = submissions.Select(s => s.Id).ToList();
            var grades = await _gradeGateway.GetBySubmissionIdsAsync(submissionIds);

            var stats = new ClassStatisticsDto
            {
                TotalEnrolledStudents = enrollments.Count,
                TotalAssignments = assignments.Count
            };

            if (grades.Any())
            {
                stats.ClassAverage = Math.Round(grades.Average(g => g.Score), 2);
                stats.HighestGrade = Math.Round(grades.Max(g => g.Score), 2);
            }

            int expectedTotalSubmissions = enrollments.Count * assignments.Count;
            int onTimeSubmissions = 0;
            int lateSubmissions = 0;

            foreach (var submission in submissions)
            {
                var parentAssignment = assignments.FirstOrDefault(a => a.Id == submission.AssignmentId);

                if (parentAssignment != null && parentAssignment.DueTime.HasValue && submission.SubmittedAt > parentAssignment.DueTime.Value)
                {
                    lateSubmissions++;
                }
                else
                {
                    onTimeSubmissions++;
                }
            }

            stats.SubmissionsOnTime = onTimeSubmissions;
            stats.SubmissionsLate = lateSubmissions;
            stats.SubmissionsMissing = Math.Max(0, expectedTotalSubmissions - (onTimeSubmissions + lateSubmissions));

            if (expectedTotalSubmissions > 0)
            {
                stats.SubmissionRate =
                    Math.Round(((double)(onTimeSubmissions + lateSubmissions) / expectedTotalSubmissions) * 100, 1);
            }

            stats.GradeDistributionLabels = new List<string>
            {
                "9.00 - 10.00",
                "8.00 - 8.99",
                "7.00 - 7.99",
                "6.00 - 6.99",
                "5.00 - 5.99",
                "1.00 - 4.99"
            };

            stats.GradeDistributionCounts = new List<int>
            {
                grades.Count(g => g.Score >= 9),
                grades.Count(g => g.Score >= 8 && g.Score < 9),
                grades.Count(g => g.Score >= 7 && g.Score < 8),
                grades.Count(g => g.Score >= 6 && g.Score < 7),
                grades.Count(g => g.Score >= 5 && g.Score < 6),
                grades.Count(g => g.Score >= 1 && g.Score < 5)
            };

            var studentSummaries = new List<StudentPerformanceSummaryDto>();

            foreach (var enrollment in enrollments)
            {
                var studentSubmissions = submissions.Where(s => s.StudentWallet == enrollment.StudentWallet).ToList();
                var studentSubmissionIds = studentSubmissions.Select(s => s.Id).ToList();
                var studentGrades = grades.Where(g => studentSubmissionIds.Contains(g.SubmissionId)).ToList();

                int missing = Math.Max(0, assignments.Count - studentSubmissions.Count);
                double average = studentGrades.Any() ? Math.Round(studentGrades.Average(g => g.Score), 2) : 0.0;

                string displayName = enrollment.StudentWallet.Length > 10
                    ? $"{enrollment.StudentWallet.Substring(0, 6)}...{enrollment.StudentWallet.Substring(enrollment.StudentWallet.Length - 4)}"
                    : enrollment.StudentWallet;

                studentSummaries.Add(new StudentPerformanceSummaryDto
                {
                    StudentWallet = enrollment.StudentWallet,
                    StudentName = displayName,
                    AverageGrade = average,
                    MissingAssignmentsCount = missing
                });
            }

            stats.TopPerformers = studentSummaries
                .Where(s => s.AverageGrade >= 9.0)
                .OrderByDescending(s => s.AverageGrade)
                .Take(MaxListedStudents)
                .ToList();

            stats.AtRiskStudents = studentSummaries
                .Where(s => (s.AverageGrade > 0 && s.AverageGrade < 5.0) || s.MissingAssignmentsCount >= 2)
                .OrderByDescending(s => s.MissingAssignmentsCount)
                .ThenBy(s => s.AverageGrade)
                .Take(MaxListedStudents)
                .ToList();

            stats.StudentsAtRiskCount = stats.AtRiskStudents.Count;

            if (grades.Any())
            {
                var earliest = grades.Min(g => g.GradedAt);
                var weeklyGroups = grades
                    .GroupBy(g => Math.Max(1, (int)Math.Ceiling((g.GradedAt - earliest).TotalDays / 7.0)))
                    .OrderBy(group => group.Key)
                    .Take(MaxWeeks)
                    .ToList();

                foreach (var group in weeklyGroups)
                {
                    stats.WeeklyLabels.Add($"Week {group.Key}");
                    stats.WeeklyAverages.Add(Math.Round(group.Average(g => g.Score), 2));
                }
            }

            return stats;
        }
    }
}
