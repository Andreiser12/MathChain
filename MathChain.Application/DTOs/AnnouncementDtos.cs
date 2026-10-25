using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathChain.Application.DTOs
{
    public class CreateAnnouncementRequest
    {
        public Guid ClassRoomId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string AuthorWallet { get; set; } = string.Empty;
    }

    public class AddCommentRequest
    {
        public Guid MessageId { get; set; }
        public string AuthorWallet { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    public class CommentResponseDto
    {
        public Guid Id { get; set; }
        public Guid MessageId { get; set; }
        public string AuthorWallet { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class AnnouncementResponseDto
    {
        public Guid Id { get; set; }
        public Guid ClassRoomId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string AuthorWallet { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<CommentResponseDto> Comments { get; set; } = new();
    }
}
