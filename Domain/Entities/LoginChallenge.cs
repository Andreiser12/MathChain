namespace MathChain.Domain.Entities
{
    public class LoginChallenge
    {
        public string WalletAddress { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTimeOffset ExpiresAt { get; set; }
    }
}
