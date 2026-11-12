using MathChain.Application.Contexts;
using MathChain.Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace MathChain.API.Controllers.MyClassesControllers
{
    [ApiController]
    [Route("api/classroom")]
    public class ClassRoomController : ApiControllerBase
    {
        private readonly IClassRoomContext _classRoomContext;
        public ClassRoomController(IClassRoomContext classRoomContext)
        {
            _classRoomContext = classRoomContext;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateClassRoom([FromBody] CreateClassRoomRequest request)
        {
            var classRoom = await _classRoomContext.CreateClassRoomAsync(request);

            return Success(classRoom, "Classroom created successfully.", StatusCodes.Status201Created);
        }

        [HttpPost("join")]
        public async Task<IActionResult> JoinClassRoom([FromBody] JoinClassRoomRequest request)
        {
            var classRoom = await _classRoomContext.JoinClassRoomAsync(request);

            return Success(classRoom, "Enrolled in classroom successfully.");
        }

        [HttpGet("by-teacher/{teacherWallet}")]
        public async Task<IActionResult> GetClassRoomByTeacher(string teacherWallet)
        {
            var classRooms = await _classRoomContext.GetByTeacherWalletAsync(teacherWallet);

            return Success(classRooms);
        }

        [HttpGet("by-student/{studentWallet}")]
        public async Task<IActionResult> GetClassRoomByStudent(string studentWallet)
        {
            var classRooms = await _classRoomContext.GetByStudentWalletAsync(studentWallet);

            return Success(classRooms);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetClassRoomById(Guid id)
        {
            var classRoom = await _classRoomContext.GetClassRoomByIdAsync(id);

            return Success(classRoom);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteClassRoom(Guid id)
        {
            await _classRoomContext.DeleteClassRoomAsync(id);

            return SuccessMessage("Classroom deleted successfully.");
        }

        [HttpGet("{classRoomId}/students")]
        public async Task<IActionResult> GetEnrolledStudents(Guid classRoomId)
        {
            var students = await _classRoomContext.GetEnrolledStudentsAsync(classRoomId);

            return Success(students);
        }
    }
}
