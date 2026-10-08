namespace MathChain.Blazor.DTOs
{
    public class SubmissionItemDto
    {
        public Guid Id { get; set; }
        public Guid AssignmentId { get; set; }
        public string StudentWallet { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string? FileName { get; set; }
        public string? IpfsHash { get; set; }
        public DateTime SubmittedAt { get; set; }
        public double? Score { get; set; }
        public string? Feedback { get; set; }
    }
}
