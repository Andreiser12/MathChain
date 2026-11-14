namespace MathChain.Infrastructure.Authentication
{
    public class GoogleAuthSettings
    {
        public string ClientId { get; set; } = string.Empty;
        public string Authority { get; set; } = "https://accounts.google.com";
        public int MaxTokenAgeMinutes { get; set; } = 10;
        public int ClockSkewSeconds { get; set; } = 60;
    }
}
