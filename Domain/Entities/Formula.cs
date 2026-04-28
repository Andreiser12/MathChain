
namespace MathChain.Domain.Entities
{
    public class Formula
    {
        public Guid Id { get; set; }
        public string Latex { get; set; } = string.Empty;
        public string ModelPath { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
