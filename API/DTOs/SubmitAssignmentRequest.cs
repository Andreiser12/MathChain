namespace MathChain.API.DTOs
{
    public class SubmitAssignmentRequest
    {
        public string StudentWallet { get; set; } = string.Empty;
        public string? FileName { get; set; }
        public string? IpfsHash { get; set; }
        public string? Content { get; set; }
    }
}
