namespace MathChain.API.DTOs
{
    public class SubmitSolutionRequest
    {
        public Guid ProblemId { get; set; }
        public string WalletAddress { get; set; }
        public double Solution { get; set; }
    }
}
