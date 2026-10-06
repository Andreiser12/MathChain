namespace MathChain.API.DTOs
{
    public class GradeSubmissionRequest
    {
        public double Score { get; set; }
        public string Feedback { get; set; } = string.Empty;
    }
}
