namespace MathChain.Application.DTOs
{
    public class SubmitSolutionRequest
    {
        public Guid ProblemId { get; set; }
        public string WalletAddress { get; set; } = string.Empty;
        public double Solution { get; set; }
    }

    public class VerifySolutionRequest
    {
        public Guid ProblemId { get; set; }
        public double Solution { get; set; }
    }

    public class PayForSolutionRequest
    {
        public Guid ProblemId { get; set; }
        public string WalletAddress { get; set; } = string.Empty;
    }
}
