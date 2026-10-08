namespace MathChain.Blazor.DTOs
{
    public class ClassResourceDto
    {
        public Guid Id { get; set; }
        public Guid ClassRoomId { get; set; }
        public int WeekNumber { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string IpfsHash { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
