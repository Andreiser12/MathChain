using MathChain.Application.Contexts;
using MathChain.Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace MathChain.API.Controllers.MyClassesControllers
{
    [ApiController]
    [Route("api/classroom")]
    public class ClassMessagesController : ApiControllerBase
    {
        private readonly IClassContentContext _contentContext;
        public ClassMessagesController(IClassContentContext contentContext)
        {
            _contentContext = contentContext;
        }

        [HttpGet("{classRoomId}/messages")]
        public async Task<IActionResult> GetMessages(Guid classRoomId)
        {
            var announcements = await _contentContext.GetAnnouncementsAsync(classRoomId);

            return Success(announcements);
        }

        [HttpPost("{classRoomId}/messages")]
        public async Task<IActionResult> CreateMessage(Guid classRoomId, [FromBody] CreateAnnouncementRequest request)
        {
            var announcement = await _contentContext.CreateAnnouncementAsync(classRoomId, request);

            return Success(announcement, "Announcement posted successfully.", StatusCodes.Status201Created);
        }

        [HttpPost("messages/{messageId:guid}/comments")]
        public async Task<IActionResult> AddComment(Guid messageId, [FromBody] AddCommentRequest request)
        {
            var comment = await _contentContext.AddCommentAsync(messageId, request);

            return Success(comment, "Comment added successfully.", StatusCodes.Status201Created);
        }
    }
}
