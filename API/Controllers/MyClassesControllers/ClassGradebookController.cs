using MathChain.Application.Contexts;
using MathChain.Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace MathChain.API.Controllers.MyClassesControllers
{
    [ApiController]
    [Route("api/classroom")]
    public class ClassGradebookController : ControllerBase
    {
        private readonly IGradebookContext _gradebookContext;

        public ClassGradebookController(IGradebookContext gradebookContext)
        {
            _gradebookContext = gradebookContext;
        }

        [HttpGet("{classRoomId}/gradebook")]
        public async Task<IActionResult> GetGradebook(Guid classRoomId)
        {
            var gradebook = await _gradebookContext.GetGradebookAsync(classRoomId);

            return Ok(gradebook);
        }

        [HttpPost("{classRoomId}/students/{studentWallet}/final-grade")]
        public async Task<IActionResult> SetFinalGrade(Guid classRoomId, string studentWallet, [FromBody] SetFinalGradeDto request)
        {
            await _gradebookContext.SetFinalGradeAsync(classRoomId, studentWallet, request.FinalGrade);

            return Ok();
        }

        [HttpDelete("{classRoomId}/students/{studentWallet}")]
        public async Task<IActionResult> RemoveStudent(Guid classRoomId, string studentWallet)
        {
            await _gradebookContext.RemoveStudentAsync(classRoomId, studentWallet);

            return Ok();
        }
    }
}
