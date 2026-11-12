using MathChain.Application.Contexts;
using MathChain.Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace MathChain.API.Controllers.MyClassesControllers
{
    [ApiController]
    [Route("api/classroom")]
    public class ClassAssignmentsController : ApiControllerBase
    {
        private readonly IAssignmentContext _assignmentContext;
        public ClassAssignmentsController(IAssignmentContext assignmentContext)
        {
            _assignmentContext = assignmentContext;
        }

        [HttpGet("{classRoomId}/assignment")]
        public async Task<IActionResult> GetAssignments(Guid classRoomId)
        {
            var assignments = await _assignmentContext.GetAssignmentsByClassRoomAsync(classRoomId);

            return Success(assignments);
        }

        [HttpPost("{classRoomId}/assignment")]
        public async Task<IActionResult> CreateAssignment(Guid classRoomId, [FromBody] CreateAssignmentRequest request)
        {
            var assignment = await _assignmentContext.CreateAssignmentAsync(classRoomId, request);

            return Success(assignment, "Assignment created successfully.", StatusCodes.Status201Created);
        }

        [HttpDelete("assignments/{assignmentId:guid}")]
        public async Task<IActionResult> DeleteAssignment(Guid assignmentId)
        {
            await _assignmentContext.DeleteAssignmentAsync(assignmentId);

            return SuccessMessage("Assignment deleted successfully.");
        }

        [HttpGet("assignment/{assignmentId:guid}/submissions")]
        public async Task<IActionResult> GetAssignmentSubmissions(Guid assignmentId)
        {
            var submissions = await _assignmentContext.GetSubmissionsAsync(assignmentId);

            return Success(submissions);
        }

        [HttpPost("assignment/{assignmentId:guid}/submit")]
        public async Task<IActionResult> SubmitAssignment(Guid assignmentId, [FromBody] SubmitAssignmentRequest request)
        {
            var submission = await _assignmentContext.SubmitAssignmentAsync(assignmentId, request);

            return Success(submission, "Assignment submitted successfully.");
        }

        [HttpPost("submissions/{submissionId:guid}/grade")]
        public async Task<IActionResult> GradeSubmission(Guid submissionId, [FromBody] GradeSubmissionRequest request)
        {
            var grade = await _assignmentContext.GradeSubmissionAsync(submissionId, request);

            return Success(grade, "Submission graded successfully.");
        }
    }
}
