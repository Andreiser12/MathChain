namespace MathChain.API.DTOs
{
    public class AddCommentRequest
    {
        public Guid MessageId { get; set; }
        public string AuthorWallet { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }
}
