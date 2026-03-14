namespace MathChain.API.DTOs
{
    public class VerifySolutionRequest
    {
        public Guid ProblemId { get; set; }
        public double Solution { get; set; }
    }
}
