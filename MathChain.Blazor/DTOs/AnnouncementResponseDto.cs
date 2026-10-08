using MathChain.Blazor.Services;

namespace MathChain.Blazor.DTOs
{
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
