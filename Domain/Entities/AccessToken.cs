namespace MathChain.Domain.Entities
{
    public class AccessToken
    {
        public string Value { get; set; } = string.Empty;
        public DateTimeOffset ExpiresAt { get; set; }
    }
}
