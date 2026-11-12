using MathChain.Application.DTOs;
using MathChain.Domain.Entities;
using MathChain.Domain.Exceptions;
using MathChain.Domain.Gateways;

namespace MathChain.Application.Contexts
{
    public class GradebookContext : IGradebookContext
    {
        private const string EnrollmentNotFoundMessage = "Student enrollment not found!";

        private readonly IClassRoomGateway _classRoomGateway;
        private readonly IAssignmentGateway _assignmentGateway;
        private readonly ISubmissionGateway _submissionGateway;
        private readonly IGradeGateway _gradeGateway;

        public GradebookContext(IClassRoomGateway classRoomGateway, IAssignmentGateway assignmentGateway,
            ISubmissionGateway submissionGateway, IGradeGateway gradeGateway)
        {
            _classRoomGateway = classRoomGateway;
            _assignmentGateway = assignmentGateway;
            _submissionGateway = submissionGateway;
            _gradeGateway = gradeGateway;
        }

        public async Task<List<StudentGradebookDto>> GetGradebookAsync(Guid classRoomId)
        {
            var enrollments = await _classRoomGateway.GetEnrollmentsAsync(classRoomId);
            var assignmentIds = await _assignmentGateway.GetAllIdsByClassRoomIdAsync(classRoomId);
            var submissions = await _submissionGateway.GetByAssignmentIdsAsync(assignmentIds);
            var submissionIds = submissions.Select(s => s.Id).ToList();
            var grades = await _gradeGateway.GetBySubmissionIdsAsync(submissionIds);

            return enrollments.Select(e =>
            {
                var studentSubmissionIds = submissions
                    .Where(s => s.StudentWallet == e.StudentWallet)
                    .Select(s => s.Id)
                    .ToList();

                var studentGrades = grades
                    .Where(g => studentSubmissionIds.Contains(g.SubmissionId))
                    .OrderBy(g => g.GradedAt)
                    .Select(g => new StudentGradeEntryDto
                    {
                        GradedAt = g.GradedAt,
                        Score = g.Score
                    })
                    .ToList();

                return new StudentGradebookDto
                {
                    StudentWallet = e.StudentWallet,
                    StudentName = FormatWallet(e.StudentWallet),
                    FinalGrade = e.FinalGrade,
                    Grades = studentGrades
                };
            }).ToList();
        }

        public async Task SetFinalGradeAsync(Guid classRoomId, string studentWallet, int finalGrade)
        {
            var enrollment = await GetEnrollmentOrThrowAsync(classRoomId, studentWallet);

            enrollment.FinalGrade = finalGrade;
            await _classRoomGateway.UpdateEnrollmentAsync(enrollment);
        }

        public async Task RemoveStudentAsync(Guid classRoomId, string studentWallet)
        {
            var enrollment = await GetEnrollmentOrThrowAsync(classRoomId, studentWallet);

            await _classRoomGateway.RemoveEnrollmentAsync(enrollment);
        }

        private async Task<Enrollment> GetEnrollmentOrThrowAsync(Guid classRoomId, string studentWallet)
        {
            var enrollment = await _classRoomGateway.GetEnrollmentAsync(classRoomId, studentWallet);

            if (enrollment == null)
            {
                throw new NotFoundException(EnrollmentNotFoundMessage);
            }

            return enrollment;
        }

        private static string FormatWallet(string wallet)
        {
            return wallet.Length > 10
                ? $"{wallet.Substring(0, 6)}...{wallet.Substring(wallet.Length - 4)}"
                : wallet;
        }
    }
}
