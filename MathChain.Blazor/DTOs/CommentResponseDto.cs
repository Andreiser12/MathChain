namespace MathChain.Blazor.DTOs
{
    public class CommentResponseDto
    {
        public Guid Id { get; set; }
        public Guid MessageId { get; set; }
        public string AuthorWallet { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
