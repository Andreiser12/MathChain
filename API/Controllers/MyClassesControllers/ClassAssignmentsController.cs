using MathChain.API.Data;
using MathChain.API.DTOs;
using MathChain.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MathChain.API.Controllers.MyClassesControllers
{
    [ApiController]
    [Route("api/classroom")]
    public class ClassAssignmentsController : ControllerBase
    {
        private readonly MathChainDbContext _context;

        public ClassAssignmentsController(MathChainDbContext context)
        {
            _context = context;
        }

        [HttpGet("{classRoomId}/assignment")]
        public async Task<IActionResult> GetAssignments(Guid classRoomId)
        {
            var assignments = await _context.Assignments
                .OfType<Assignment>()
                .Where(a => a.ClassRoomId == classRoomId)
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => new AssignmentResponseDto
                {
                    Id = a.Id,
                    ClassRoomId = a.ClassRoomId,
                    Title = a.Title,
                    Description = a.Description,
                    StartTime = a.StartTime,
                    DueTime = a.DueTime,
                    FileName = a.FileName,
                    IpfsHash = a.IpfsHash,
                    CreatedAt = a.CreatedAt,
                    SubmissionCount = _context.Submissions.Count(s => s.AssignmentId == a.Id)
                })
                .ToListAsync();

            return Ok(assignments);
        }

        [HttpPost("{classRoomId}/assignment")]
        public async Task<IActionResult> CreateAssignment(Guid classRoomId, [FromBody] CreateAssignmentRequest request)
        {
            var classRoom = await _context.ClassRooms.FindAsync(classRoomId);

            if (classRoom == null)
            {
                return NotFound("Class not found!");
            }

            var assignment = new Assignment
            {
                Id = Guid.NewGuid(),
                ClassRoomId = classRoomId,
                Title = request.Title,
                Description = request.Description,
                StartTime = request.StartTime,
                DueTime = request.DueTime,
                FileName = request.FileName,
                IpfsHash = request.IpfsHash,
                CreatedAt = DateTime.UtcNow
            };

            _context.Assignments.Add(assignment);
            await _context.SaveChangesAsync();

            return Ok(assignment);
        }

        [HttpDelete("assignments/{assignmentId:guid}")]
        public async Task<IActionResult> DeleteAssignment(Guid assignmentId)
        {
            var assignment = await _context.Assignments
                .OfType<Assignment>()
                .FirstOrDefaultAsync(a => a.Id == assignmentId);

            if (assignment == null)
            {
                return NotFound("Assignment not found!");
            }

            var submissions = await _context.Submissions
                .Where(s => s.AssignmentId == assignmentId)
                .ToListAsync();

            var submissionIds = submissions.Select(s => s.Id).ToList();
            var grades = await _context.Grades
                .Where(g => submissionIds.Contains(g.SubmissionId))
                .ToListAsync();

            _context.Grades.RemoveRange(grades);
            _context.Submissions.RemoveRange(submissions);
            _context.Assignments.Remove(assignment);

            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpGet("assignment/{assignmentId:guid}/submissions")]
        public async Task<IActionResult> GetAssignmentSubmissions(Guid assignmentId)
        {
            var submissions = await _context.Submissions
                .Where(s => s.AssignmentId == assignmentId)
                .OrderBy(s => s.SubmittedAt)
                .ToListAsync();

            var submissionsIds = submissions.Select(s => s.Id).ToList();

            var grades = await _context.Grades
                .Where(g => submissionsIds.Contains(g.SubmissionId))
                .ToListAsync();

            var result = submissions.Select(s =>
            {
                var grade = grades.FirstOrDefault(g => g.SubmissionId == s.Id);
                return new SubmissionResponseDto
                {
                    Id = s.Id,
                    AssignmentId = s.AssignmentId,
                    StudentWallet = s.StudentWallet,
                    StudentName = s.StudentWallet.Length > 10 ?
                    $"{s.StudentWallet.Substring(0, 6)}...{s.StudentWallet.Substring(s.StudentWallet.Length - 4)}"
                    : s.StudentWallet,
                    FileName = s.FileName,
                    IpfsHash = s.IpfsHash,
                    SubmittedAt = s.SubmittedAt,
                    Score = grade?.Score,
                    Feedback = grade?.Feedback
                };
            }).ToList();

            return Ok(result);
        }

        [HttpPost("submissions/{submissionId:guid}/grade")]
        public async Task<IActionResult> GradeSubmission(Guid submissionId, [FromBody] GradeSubmissionRequest request)
        {
            var submission = await _context.Submissions.FindAsync(submissionId);

            if (submission == null)
            {
                return NotFound("Submission not found!");
            }

            var existingGrade = await _context.Grades
                .FirstOrDefaultAsync(g => g.SubmissionId == submissionId);

            if (existingGrade != null)
            {
                existingGrade.Score = request.Score;
                existingGrade.Feedback = request.Feedback;
                existingGrade.GradedAt = DateTime.UtcNow;
            }
            else
            {
                var grade = new Grade
                {
                    Id = Guid.NewGuid(),
                    SubmissionId = submissionId,
                    Score = request.Score,
                    Feedback = request.Feedback,
                    GradedAt = DateTime.UtcNow
                };

                _context.Grades.Add(grade);
            }

            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}
