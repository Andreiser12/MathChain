namespace MathChain.Infrastructure.Authentication
{
    public class JwtSettings
    {
        public string Key { get; set; } = string.Empty;
        public string Issuer { get; set; } = "MathChain.API";
        public string Audience { get; set; } = "MathChain.Blazor";
        public int LifetimeMinutes { get; set; } = 60;
        public int ClockSkewSeconds { get; set; } = 60;
    }
}
