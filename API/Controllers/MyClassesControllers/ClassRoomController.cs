using MathChain.Application.Contexts;
using MathChain.Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace MathChain.API.Controllers.MyClassesControllers
{
    [ApiController]
    [Route("api/classroom")]
    public class ClassRoomController : ControllerBase
    {
        private readonly IClassRoomContext _classRoomContext;
        public ClassRoomController(IClassRoomContext classRoomContext)
        {
            _classRoomContext = classRoomContext;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateClassRoom([FromBody] CreateClassRoomRequest request)
        {
            var result = await _classRoomContext.CreateClassRoomAsync(request);

            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("join")]
        public async Task<IActionResult> JoinClassRoom([FromBody] JoinClassRoomRequest request)
        {
            var result = await _classRoomContext.JoinClassRoomAsync(request);

            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("by-teacher/{teacherWallet}")]
        public async Task<IActionResult> GetClassRoomByTeacher(string teacherWallet)
        {
            var result = await _classRoomContext.GetByTeacherWalletAsync(teacherWallet);

            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("by-student/{studentWallet}")]
        public async Task<IActionResult> GetClassRoomByStudent(string studentWallet)
        {
            var result = await _classRoomContext.GetByStudentWalletAsync(studentWallet);

            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetClassRoomById(Guid id)
        {
            var result = await _classRoomContext.GetClassRoomByIdAsync(id);

            return StatusCode(result.StatusCode, result);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteClassRoom(Guid id)
        {
            var result = await _classRoomContext.DeleteClassRoomAsync(id);

            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("{classRoomId}/students")]
        public async Task<IActionResult> GetEnrolledStudents(Guid classRoomId)
        {
            var result = await _classRoomContext.GetEnrolledStudentsAsync(classRoomId);

            return StatusCode(result.StatusCode, result);
        }
    }
}
