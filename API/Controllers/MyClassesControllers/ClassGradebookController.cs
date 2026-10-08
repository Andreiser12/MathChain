using MathChain.API.Data;
using MathChain.API.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MathChain.API.Controllers.MyClassesControllers
{
    [ApiController]
    [Route("api/classroom")]
    public class ClassGradebookController : ControllerBase
    {
        private readonly MathChainDbContext _context;

        public ClassGradebookController(MathChainDbContext context)
        {
            _context = context;
        }

        [HttpGet("{classRoomId}/gradebook")]
        public async Task<IActionResult> GetGradebook(Guid classRoomId)
        {
            var enrollments = await _context.Enrollments
                .Where(e => e.ClassRoomId == classRoomId)
                .ToListAsync();

            var assignmentIds = await _context.Assignments
                .Where(a => a.ClassRoomId == classRoomId)
                .Select(a => a.Id)
                .ToListAsync();

            var submissions = await _context.Submissions
                .Where(s => assignmentIds.Contains(s.AssignmentId))
                .ToListAsync();

            var submissionIds = submissions.Select(s => s.Id).ToList();

            var grades = await _context.Grades
                .Where(g => submissionIds.Contains(g.SubmissionId))
                .ToListAsync();

            var result = enrollments.Select(e =>
            {
                var studentSubmissions = submissions.Where(s => s.StudentWallet == e.StudentWallet)
                .Select(s => s.Id).ToList();

                var studentGrades = grades
                .Where(g => studentSubmissions.Contains(g.SubmissionId))
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
                    StudentName = e.StudentWallet.Length > 10 ?
                    $"{e.StudentWallet.Substring(0, 6)}...{e.StudentWallet.Substring(e.StudentWallet.Length - 4)}"
                        : e.StudentWallet,
                    FinalGrade = e.FinalGrade,
                    Grades = studentGrades
                };
            })
            .ToList();

            return Ok(result);
        }

        [HttpPost("{classRoomId}/students/{studentWallet}/final-grade")]
        public async Task<IActionResult> SetFinalGrade(Guid classRoomId, string studentWallet, [FromBody] SetFinalGradeDto request)
        {
            var enrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e => e.ClassRoomId == classRoomId && e.StudentWallet == studentWallet);

            if (enrollment == null)
            {
                return NotFound("Student enrollment not found!");
            }

            enrollment.FinalGrade = request.FinalGrade;
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpDelete("{classRoomId}/students/{studentWallet}")]
        public async Task<IActionResult> RemoveStudent(Guid classRoomId, string studentWallet)
        {
            var enrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e => e.ClassRoomId == classRoomId && e.StudentWallet == studentWallet);

            if (enrollment == null)
            {
                return NotFound("Student enrollment not found!");
            }

            _context.Enrollments.Remove(enrollment);
            await _context.SaveChangesAsync();

            return Ok();
        }
    }
}
