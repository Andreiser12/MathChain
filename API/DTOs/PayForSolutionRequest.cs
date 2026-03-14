namespace MathChain.API.DTOs
{
    public class PayForSolutionRequest
    {
        public Guid ProblemId { get; set; }
        public string WalletAddress { get; set; }
    }
}
