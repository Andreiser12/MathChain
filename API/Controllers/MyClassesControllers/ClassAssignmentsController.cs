using MathChain.Application.Contexts;
using MathChain.Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace MathChain.API.Controllers.MyClassesControllers
{
    [ApiController]
    [Route("api/classroom")]
    public class ClassAssignmentsController : ControllerBase
    {
        private readonly IAssignmentContext _assignmentContext;
        public ClassAssignmentsController(IAssignmentContext assignmentContext)
        {
            _assignmentContext = assignmentContext;
        }

        [HttpGet("{classRoomId}/assignment")]
        public async Task<IActionResult> GetAssignments(Guid classRoomId)
        {
            var result = await _assignmentContext.GetAssignmentsByClassRoomAsync(classRoomId);

            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("{classRoomId}/assignment")]
        public async Task<IActionResult> CreateAssignment(Guid classRoomId, [FromBody] CreateAssignmentRequest request)
        {
            var result = await _assignmentContext.CreateAssignmentAsync(classRoomId, request);

            return StatusCode(result.StatusCode, result);
        }

        [HttpDelete("assignments/{assignmentId:guid}")]
        public async Task<IActionResult> DeleteAssignment(Guid assignmentId)
        {
            var result = await _assignmentContext.DeleteAssignmentAsync(assignmentId);

            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("assignment/{assignmentId:guid}/submissions")]
        public async Task<IActionResult> GetAssignmentSubmissions(Guid assignmentId)
        {
            var result = await _assignmentContext.GetSubmissionsAsync(assignmentId);

            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("assignment/{assignmentId:guid}/submit")]
        public async Task<IActionResult> SubmitAssignment(Guid assignmentId, [FromBody] SubmitAssignmentRequest request)
        {
            var result = await _assignmentContext.SubmitAssignmentAsync(assignmentId, request);

            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("submissions/{submissionId:guid}/grade")]
        public async Task<IActionResult> GradeSubmission(Guid submissionId, [FromBody] GradeSubmissionRequest request)
        {
            var result = await _assignmentContext.GradeSubmissionAsync(submissionId, request);

            return StatusCode(result.StatusCode, result);
        }
    }
}
