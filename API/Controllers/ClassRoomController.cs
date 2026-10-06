using MathChain.API.Data;
using MathChain.API.DTOs;
using MathChain.Domain.Entities;
using MathNet.Numerics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Runtime.CompilerServices;

namespace MathChain.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClassRoomController : ControllerBase
    {
        private readonly MathChainDbContext _context;

        public ClassRoomController(MathChainDbContext context)
        {
            _context = context;
        }

        private async Task<string> GenerateJoinCode()
        {
            const string allowedChars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var random = new Random();
            string code;
            bool exists = false;

            do
            {
                code = new string(Enumerable.Range(0, 6)
                    .Select(_ => allowedChars[random.Next(allowedChars.Length)])
                    .ToArray());

                exists = await _context.ClassRooms.AnyAsync(c => c.JoinCode == code);
            } while (exists);

            return code;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateClassRoom([FromBody] CreateClassRoomRequest request)
        {
            var classRoom = new ClassRoom
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                TeacherWallet = request.TeacherWallet,
                JoinCode = await GenerateJoinCode(),
                CreatedAt = DateTime.UtcNow
            };

            _context.ClassRooms.Add(classRoom);
            await _context.SaveChangesAsync();

            return Ok(classRoom);
        }

        [HttpPost("join")]
        public async Task<IActionResult> JoinClassRoom([FromBody] JoinClassRoomRequest request)
        {
            var classRoom = await _context.ClassRooms
                .FirstOrDefaultAsync(c => c.JoinCode == request.JoinCode);

            if (classRoom == null)
            {
                return NotFound("Class not found!");
            }

            var alreadyEnrolled = await _context.Enrollments
                .AnyAsync(e => e.ClassRoomId == classRoom.Id && e.StudentWallet == request.StudentWallet);

            if (alreadyEnrolled)
            {
                return BadRequest("You are already enrolled in this class!");
            }

            var enrollment = new Enrollment
            {
                Id = Guid.NewGuid(),
                ClassRoomId = classRoom.Id,
                StudentWallet = request.StudentWallet,
                CreatedAt = DateTime.UtcNow
            };

            _context.Enrollments.Add(enrollment);
            await _context.SaveChangesAsync();

            return Ok(classRoom);
        }

        [HttpGet("by-teacher/{teacherWallet}")]
        public async Task<IActionResult> GetClassRoomByTeacher(string teacherWallet)
        {
            var classRoom = await _context.ClassRooms
                .Where(c => c.TeacherWallet == teacherWallet)
                .ToListAsync();

            return Ok(classRoom);
        }

        [HttpGet("by-student/{studentWallet}")]
        public async Task<IActionResult> GetClassRoomByStudent(string studentWallet)
        {
            var classRoom = await _context.Enrollments
                .Where(e => e.StudentWallet == studentWallet)
                .Join(_context.ClassRooms,
                enrollment => enrollment.ClassRoomId,
                classRoom => classRoom.Id,
                (enrollment, classRoom) => classRoom)
                .ToListAsync();

            return Ok(classRoom);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetClassRoomById(Guid id)
        {
            var classRoom = await _context.ClassRooms.FindAsync(id);
            if (classRoom == null)
            {
                return NotFound("Class not found!");
            }

            return Ok(classRoom);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteClassRoom(Guid id)
        {
            var classRoom = await _context.ClassRooms.FindAsync(id);
            if (classRoom == null)
            {
                return NotFound("Class not found!");
            }

            var enrollments = await _context.Enrollments.Where(e => e.ClassRoomId == id).ToListAsync();
            _context.Enrollments.RemoveRange(enrollments);

            _context.ClassRooms.Remove(classRoom);
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpGet("{classRoomId}/students")]
        public async Task<IActionResult> GetEnrolledStudents(Guid classRoomId)
        {
            var students = await _context.Enrollments
                .Where(e => e.ClassRoomId == classRoomId)
                .Select(e => e.StudentWallet)
                .ToListAsync();

            return Ok(students);
        }

        [HttpGet("{classRoomId}/materials")]
        public async Task<IActionResult> GetMaterials(Guid classRoomId)
        {
            var materials = await _context.ClassMaterials
                .Where(m => m.ClassRoomId == classRoomId)
                .OrderBy(m => m.WeekNumber)
                .ThenBy(m => m.CreatedAt)
                .ToListAsync();

            return Ok(materials);
        }

        [HttpPost("{classRoomId}/materials")]
        public async Task<IActionResult> AddMaterial(Guid classRoomId, [FromBody] CreateMaterialRequest request)
        {
            var classRoom = await _context.ClassRooms.FindAsync(classRoomId);
            if (classRoom == null)
            {
                return NotFound("Class not found!");
            }

            var material = new ClassResource
            {
                Id = Guid.NewGuid(),
                ClassRoomId = classRoomId,
                WeekNumber = request.WeekNumber,
                Title = request.Title,
                Description = request.Description,
                FileName = request.FileName,
                IpfsHash = request.IpfsHash,
                CreatedAt = DateTime.UtcNow
            };

            _context.ClassMaterials.Add(material);
            await _context.SaveChangesAsync();

            return Ok(material);
        }

        [HttpDelete("materials/{materialId:guid}")]
        public async Task<IActionResult> DeleteMaterial(Guid materialId)
        {
            var material = await _context.ClassMaterials.FindAsync(materialId);
            if (material == null)
            {
                return NotFound("Material not found!");
            }

            _context.ClassMaterials.Remove(material);
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpGet("{classRoomId}/assignment")]
        public async Task<IActionResult> GetAssignments(Guid classRoomId)
        {
            var assignments = await _context.Assignments
                .OfType<Assignment>()
                .Where(a => a.ClassRoomId == classRoomId)
                .OrderByDescending(a => a.CreatedAt)
                .Select(a=> new AssignmentResponseDto
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

            if(classRoom == null)
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

            if(assignment == null)
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

            if(submission == null)
            {
                return NotFound("Submission not found!");
            }

            var existingGrade = await _context.Grades
                .FirstOrDefaultAsync(g => g.SubmissionId == submissionId);

            if(existingGrade != null)
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

            if(enrollment == null)
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

            if(enrollment == null)
            {
                return NotFound("Student enrollment not found!");
            }

            _context.Enrollments.Remove(enrollment);
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpGet("{classRoomId}/messages")]
        public async Task<IActionResult> GetMessages(Guid classRoomId)
        {
            var announcements = await _context.ClassAnnouncements
                .Where(a => a.ClassRoomId == classRoomId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            var messageIds = announcements.Select(a => a.Id).ToList();
            
            var comments = await _context.MessagesComments
                .Where(c => messageIds.Contains(c.MessageId))
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();

            var result = announcements.Select(a => new AnnouncementResponseDto
            {
                Id = a.Id,
                ClassRoomId = a.ClassRoomId,
                Title = a.Title,
                Content = a.Content,
                AuthorWallet = a.AuthorWallet,
                AuthorName = a.AuthorWallet.Length > 10
                    ? $"{a.AuthorWallet.Substring(0, 6)}...{a.AuthorWallet.Substring(a.AuthorWallet.Length - 4)}"
                    : a.AuthorWallet,
                CreatedAt = a.CreatedAt,
                Comments = comments.Where(c => c.MessageId == a.Id)
                .Select(c => new CommentResponseDto
                {
                    Id = c.Id,
                    MessageId = c.MessageId,
                    AuthorWallet = c.AuthorWallet,
                    AuthorName = c.AuthorWallet.Length > 10
                        ? $"{c.AuthorWallet.Substring(0, 6)}...{c.AuthorWallet.Substring(c.AuthorWallet.Length - 4)}"
                        : c.AuthorWallet,
                    Content = c.Content,
                    CreatedAt = c.CreatedAt
                }).ToList()
            }).ToList();

            return Ok(result);
        }

        [HttpPost("{classRoomId}/messages")]
        public async Task<IActionResult> CreateMessage(Guid classRoomId, [FromBody] CreateAnnouncementRequest request)
        {
            var announcement = new ClassAnnouncement
            {
                Id = Guid.NewGuid(),
                ClassRoomId = classRoomId,
                Title = request.Title,
                Content = request.Content,
                AuthorWallet = request.AuthorWallet,
                CreatedAt = DateTime.UtcNow
            };

            _context.ClassAnnouncements.Add(announcement);
            await _context.SaveChangesAsync();

            return Ok(announcement);
        }

        [HttpDelete("messages/{messageId:guid}")]
        public async Task<IActionResult> DeleteMessage(Guid messageId)
        {
            var announcement = await _context.ClassAnnouncements.FindAsync(messageId);

            if(announcement == null)
            {
                return NotFound("Message not found!");
            }

            var comments = await _context.MessagesComments.Where(c => c.MessageId == messageId).ToListAsync();
            _context.MessagesComments.RemoveRange(comments);
            _context.ClassAnnouncements.Remove(announcement);
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpPost("messages/{messageId:guid}/comments")]
        public async Task<IActionResult> AddComment(Guid messageId, [FromBody] AddCommentRequest request)
        {
            var comment = new MessageComment
            {
                Id = Guid.NewGuid(),
                MessageId = messageId,
                AuthorWallet = request.AuthorWallet,
                Content = request.Content,
                CreatedAt = DateTime.UtcNow
            };

            _context.MessagesComments.Add(comment);
            await _context.SaveChangesAsync();

            return Ok(comment);
        }

        [HttpGet("{classRoomId}/media")]
        public async Task<IActionResult> GetClassMedia(Guid classRoomId)
        {
            var mediaList = await _context.ClassMedia
                .Where(m => m.ClassRoomId == classRoomId)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            return Ok(mediaList);
        }

        [HttpPost("{classRoomId}/media")]
        public async Task<IActionResult> AddClassMedia(Guid classRoomId, [FromBody] CreateClassMediaRequest request)
        {
            var media = new ClassMedia
            {
                Id = Guid.NewGuid(),
                ClassRoomId = classRoomId,
                FileName = request.FileName,
                IpfsHash = request.IpfsHash,
                UploaderWallet = request.UploaderWallet,
                CreatedAt = DateTime.UtcNow
            };

            _context.ClassMedia.Add(media);
            await _context.SaveChangesAsync();

            return Ok(media);
        }

        [HttpDelete("media/{mediaId:guid}")]
        public async Task<IActionResult> DeleteClassMedia(Guid mediaId)
        {
            var media = await _context.ClassMedia.FindAsync(mediaId);
            if (media == null)
            {
                return NotFound("Media file not found!");
            }

            _context.ClassMedia.Remove(media);
            await _context.SaveChangesAsync();

            return Ok();
        }
    }
}
