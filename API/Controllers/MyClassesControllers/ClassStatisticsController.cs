using MathChain.Persistence.Context;
using MathChain.Application.DTOs;
using MathChain.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MathChain.API.Controllers.MyClassesControllers
{
    [ApiController]
    [Route("api/classroom")]
    public class ClassStatisticsController : ControllerBase
    {
        private readonly MathChainDbContext _context;

        public ClassStatisticsController(MathChainDbContext context)
        {
            _context = context;
        }

        [HttpGet("{classRoomId}/statistics")]
        public async Task<IActionResult> GetClassStatistics(Guid classRoomId)
        {
            var enrollments = await _context.Enrollments
                .Where(e => e.ClassRoomId == classRoomId)
                .ToListAsync();

            var assignments = await _context.Assignments
                .OfType<Assignment>()
                .Where(a => a.ClassRoomId == classRoomId)
                .ToListAsync();

            var assignmentsIds = assignments.Select(a => a.Id).ToList();

            var submissions = await _context.Submissions
                .Where(s => assignmentsIds.Contains(s.AssignmentId))
                .ToListAsync();

            var submissionIds = submissions.Select(s => s.Id).ToList();

            var grades = await _context.Grades
                .Where(g => submissionIds.Contains(g.SubmissionId))
                .ToListAsync();

            var stats = new ClassStatisticsDto
            {
                TotalEnrolledStudents = enrollments.Count,
                TotalAssignments = assignments.Count
            };

            if(grades.Any())
            {
                stats.ClassAverage = Math.Round(grades.Average(g => g.Score), 2);
                stats.HighestGrade = Math.Round(grades.Max(g => g.Score), 2);
            }

            int expectedTotalSubmissions = enrollments.Count * assignments.Count;
            int onTimeSubmissions = 0;
            int lateSubmissions = 0;

            foreach(var submission in submissions)
            {
                var parentAssignment = assignments.FirstOrDefault(a => a.Id == submission.AssignmentId);

                if(parentAssignment != null && parentAssignment.DueTime.HasValue && submission.SubmittedAt > parentAssignment.DueTime.Value)
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

            if(expectedTotalSubmissions > 0)
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

            int count9to10 = grades.Count(g => g.Score >= 9);
            int count8to9 = grades.Count(g => g.Score >= 8 && g.Score < 9);
            int count7to8 = grades.Count(g => g.Score >= 7 && g.Score < 8);
            int count6to7 = grades.Count(g => g.Score >= 6 && g.Score < 7);
            int count5to6 = grades.Count(g => g.Score >= 5 && g.Score < 6);
            int count1to5 = grades.Count(g => g.Score >= 1 && g.Score < 5);

            stats.GradeDistributionCounts = new List<int>
            {
                count9to10,
                count8to9,
                count7to8,
                count6to7,
                count5to6,
                count1to5
            };

            var studentSummaries = new List<StudentPerformanceSummaryDto>();

            foreach(var enrollment in enrollments)
            {
                var studentSubmissions = submissions.Where(s => s.StudentWallet == enrollment.StudentWallet).ToList();
                var studentSubmissionIds = studentSubmissions.Select(s => s.Id).ToList();
                var studentGrades = grades.Where(g => studentSubmissionIds.Contains(g.SubmissionId)).ToList();

                int missing = Math.Max(0, assignments.Count - studentSubmissions.Count);
                double average = studentGrades.Any() ? Math.Round(studentGrades.Average(g => g.Score), 2) : 0.0;

                string displayName =
                    enrollment.StudentWallet.Length > 10 ?
                    $"{enrollment.StudentWallet.Substring(0, 6)}...{enrollment.StudentWallet.Substring(enrollment.StudentWallet.Length - 4)}"
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
                .Take(5)
                .ToList();

            stats.AtRiskStudents = studentSummaries
                .Where(s => (s.AverageGrade > 0 && s.AverageGrade < 5.0) || s.MissingAssignmentsCount >= 2)
                .OrderByDescending(s => s.MissingAssignmentsCount)
                .ThenBy(s => s.AverageGrade)
                .Take(5)
                .ToList();

            stats.StudentsAtRiskCount = stats.AtRiskStudents.Count;

            if(grades.Any())
            {
                var earliest = grades.Min(g => g.GradedAt);
                var weeklyGroups = grades
                    .GroupBy(g => Math.Max(1, (int)Math.Ceiling((g.GradedAt - earliest).TotalDays / 7.0)))
                    .OrderBy(grp => grp.Key)
                    .Take(6)
                    .ToList();

                foreach (var grp in weeklyGroups)
                {
                    stats.WeeklyLabels.Add($"Week {grp.Key}");
                    stats.WeeklyAverages.Add(Math.Round(grp.Average(g => g.Score), 2));
                }
            }

            return Ok(stats);
        }
    }
}
