namespace MathChain.API.DTOs
{
    public class CreateAssignmentRequest
    {
        public Guid ClassRoomId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime? DueTime { get; set; }
        public string? FileName { get; set; }
        public string? IpfsHash { get; set; }
    }
}
