using MathChain.API.Data;
using MathChain.API.DTOs;
using MathChain.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MathChain.API.Controllers.MyClassesControllers
{
    [ApiController]
    [Route("api/classroom")]
    public class ClassMessagesController : ControllerBase
    {
        private readonly MathChainDbContext _context;

        public ClassMessagesController(MathChainDbContext context)
        {
            _context = context;
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

            if (announcement == null)
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
    }
}
