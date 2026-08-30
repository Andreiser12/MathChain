namespace MathChain.API.DTOs
{
    public class LoginRequest
    {
        public string WalletAddress { get; set; } = string.Empty;
        public string? Role { get; set; }
    }
}
