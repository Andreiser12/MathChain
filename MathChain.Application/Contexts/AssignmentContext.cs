using MathChain.Application.DTOs;
using MathChain.Domain.Entities;
using MathChain.Domain.Gateways;
using MathChain.Domain.Exceptions;

namespace MathChain.Application.Contexts
{
    public class AssignmentContext : IAssignmentContext
    {
        private readonly IAssignmentGateway _assignmentGateway;
        private readonly ISubmissionGateway _submissionGateway;
        private readonly IGradeGateway _gradeGateway;
        private readonly IClassRoomGateway _classRoomGateway;
        public AssignmentContext(IAssignmentGateway assignmentGateway, ISubmissionGateway submissionGateway,
            IGradeGateway gradeGateway, IClassRoomGateway classRoomGateway)
        {
            _assignmentGateway = assignmentGateway;
            _submissionGateway = submissionGateway;
            _gradeGateway = gradeGateway;
            _classRoomGateway = classRoomGateway;
        }

        public async Task<List<AssignmentResponseDto>> GetAssignmentsByClassRoomAsync(Guid classRoomId)
        {
            var assignments = await _assignmentGateway.GetByClassRoomIdAsync(classRoomId);

            var dtos = new List<AssignmentResponseDto>();

            foreach (var assignment in assignments)
            {
                var submissions = await _submissionGateway.GetByAssignmentIdAsync(assignment.Id);

                dtos.Add(new AssignmentResponseDto
                {
                    Id = assignment.Id,
                    ClassRoomId = assignment.ClassRoomId,
                    Title = assignment.Title,
                    Description = assignment.Description,
                    StartTime = assignment.StartTime,
                    DueTime = assignment.DueTime,
                    FileName = assignment.FileName,
                    IpfsHash = assignment.IpfsHash,
                    CreatedAt = assignment.CreatedAt,
                    SubmissionCount = submissions.Count
                });
            }

            return dtos;
        }

        public async Task<Assignment> GetAssignmentByIdAsync(Guid id)
        {
            var assignment = await _assignmentGateway.GetByIdAsync(id);

            if (assignment == null)
            {
                throw new NotFoundException("Assignment not found.");
            }

            return assignment;
        }

        public async Task<Assignment> CreateAssignmentAsync(Guid classRoomId, CreateAssignmentRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Title))
            {
                throw new BusinessRuleException("Assignment title is required.");
            }

            var classRoom = await _classRoomGateway.GetByIdAsync(classRoomId);

            if (classRoom == null)
            {
                throw new NotFoundException("Classroom not found.");
            }

            var assignment = new Assignment
            {
                Id = Guid.NewGuid(),
                ClassRoomId = classRoomId,
                Title = request.Title.Trim(),
                Description = request.Description?.Trim() ?? string.Empty,
                StartTime = request.StartTime,
                DueTime = request.DueTime,
                FileName = request.FileName,
                IpfsHash = request.IpfsHash,
                CreatedAt = DateTime.UtcNow
            };

            var created = await _assignmentGateway.CreateAsync(assignment);

            return created;
        }

        public async Task DeleteAssignmentAsync(Guid id)
        {
            var assignment = await _assignmentGateway.GetByIdAsync(id);

            if (assignment == null)
            {
                throw new NotFoundException("Assignment not found.");
            }

            await _assignmentGateway.DeleteAsync(id);
        }

        public async Task<List<SubmissionResponseDto>> GetSubmissionsAsync(Guid assignmentId)
        {
            var submissions = await _submissionGateway.GetByAssignmentIdAsync(assignmentId);
            var submissionIds = submissions.Select(s => s.Id).ToList();
            var grades = await _gradeGateway.GetBySubmissionIdsAsync(submissionIds);

            var dtos = submissions.Select(s =>
            {
                var grade = grades.FirstOrDefault(g => g.SubmissionId == s.Id);

                return new SubmissionResponseDto
                {
                    Id = s.Id,
                    AssignmentId = s.AssignmentId,
                    StudentWallet = s.StudentWallet,
                    StudentName = FormatWallet(s.StudentWallet),
                    FileName = s.FileName,
                    IpfsHash = s.IpfsHash,
                    SubmittedAt = s.SubmittedAt,
                    Score = grade?.Score,
                    Feedback = grade?.Feedback
                };
            }).ToList();

            return dtos;
        }

        public async Task<Submission> SubmitAssignmentAsync(Guid assignmentId, SubmitAssignmentRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.StudentWallet))
            {
                throw new BusinessRuleException("Student wallet address is required.");
            }

            var assignment = await _assignmentGateway.GetByIdAsync(assignmentId);

            if (assignment == null)
            {
                throw new NotFoundException("Assignment not found.");
            }

            var studentWallet = request.StudentWallet.Trim().ToLower();

            var existingSubmission = await _submissionGateway.GetByStudentAndAssignmentAsync(assignmentId, studentWallet);

            Submission submissionToSave;

            if (existingSubmission != null)
            {
                existingSubmission.FileName = request.FileName;
                existingSubmission.IpfsHash = request.IpfsHash;
                existingSubmission.Content = request.Content ?? string.Empty;
                existingSubmission.SubmittedAt = DateTime.UtcNow;
                submissionToSave = existingSubmission;
            }
            else
            {
                submissionToSave = new Submission
                {
                    Id = Guid.NewGuid(),
                    AssignmentId = assignmentId,
                    StudentWallet = studentWallet,
                    FileName = request.FileName,
                    IpfsHash = request.IpfsHash,
                    Content = request.Content ?? string.Empty,
                    SubmittedAt = DateTime.UtcNow
                };
            }

            var saved = await _submissionGateway.SaveAsync(submissionToSave);

            return saved;
        }

        public async Task<Grade> GradeSubmissionAsync(Guid submissionId, GradeSubmissionRequest request)
        {
            if (request.Score < 0 || request.Score > 100)
            {
                throw new BusinessRuleException("Grade score must be between 0 and 100.");
            }

            var submission = await _submissionGateway.GetByIdAsync(submissionId);

            if (submission == null)
            {
                throw new NotFoundException("Submission not found.");
            }

            var existingGrade = await _gradeGateway.GetBySubmissionIdAsync(submissionId);

            Grade gradeToSave;

            if (existingGrade != null)
            {
                existingGrade.Score = request.Score;
                existingGrade.Feedback = request.Feedback ?? string.Empty;
                existingGrade.GradedAt = DateTime.UtcNow;
                gradeToSave = existingGrade;
            }
            else
            {
                gradeToSave = new Grade
                {
                    Id = Guid.NewGuid(),
                    SubmissionId = submissionId,
                    Score = request.Score,
                    Feedback = request.Feedback ?? string.Empty,
                    GradedAt = DateTime.UtcNow
                };
            }

            var saved = await _gradeGateway.SaveAsync(gradeToSave);

            return saved;
        }

        private static string FormatWallet(string wallet)
        {
            if (string.IsNullOrWhiteSpace(wallet))
            {
                return string.Empty;
            }

            return wallet.Length > 10 ? $"{wallet[..6]}...{wallet[^4..]}" : wallet;
        }
    }
}
