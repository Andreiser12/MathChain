namespace MathChain.Domain.Entities
{
    public class ExternalLogin
    {
        public const string GoogleProvider = "google";

        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Provider { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
