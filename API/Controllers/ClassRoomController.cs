using MathChain.API.Data;
using MathChain.API.DTOs;
using MathChain.Domain.Entities;
using MathNet.Numerics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

            var material = new ClassMaterial
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
    }
}
