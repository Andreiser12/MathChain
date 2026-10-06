namespace MathChain.API.DTOs
{
    public class AssignmentResponseDto
    {
        public Guid Id { get; set; }
        public Guid ClassRoomId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime? DueTime { get; set; }
        public string? FileName { get; set; }
        public string? IpfsHash { get; set; }
        public DateTime CreatedAt { get; set; }
        public int SubmissionCount { get; set; }
    }
}
