using MathChain.Application.Contexts;
using MathChain.Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace MathChain.API.Controllers.MyClassesControllers
{
    [ApiController]
    [Route("api/classroom")]
    public class ClassMessagesController : ControllerBase
    {
        private readonly IClassContentContext _contentContext;
        public ClassMessagesController(IClassContentContext contentContext)
        {
            _contentContext = contentContext;
        }

        [HttpGet("{classRoomId}/messages")]
        public async Task<IActionResult> GetMessages(Guid classRoomId)
        {
            var result = await _contentContext.GetAnnouncementsAsync(classRoomId);

            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("{classRoomId}/messages")]
        public async Task<IActionResult> CreateMessage(Guid classRoomId, [FromBody] CreateAnnouncementRequest request)
        {
            var result = await _contentContext.CreateAnnouncementAsync(classRoomId, request);

            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("messages/{messageId:guid}/comments")]
        public async Task<IActionResult> AddComment(Guid messageId, [FromBody] AddCommentRequest request)
        {
            var result = await _contentContext.AddCommentAsync(messageId, request);

            return StatusCode(result.StatusCode, result);
        }
    }
}
