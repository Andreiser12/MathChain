namespace MathChain.API.DTOs
{
    public class CreateAnnouncementRequest
    {
        public Guid ClassRoomId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string AuthorWallet { get; set; } = string.Empty;

    }
}
