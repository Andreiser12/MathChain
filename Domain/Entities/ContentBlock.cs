namespace MathChain.Domain.Entities
{
    public class ContentBlock
    {
        public string Cid { get; set; } = string.Empty;
        public int Codec { get; set; }
        public byte[] Data { get; set; } = Array.Empty<byte>();
        public int Size { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public sealed record ContentReference(string Source, Guid OwnerId, string Cid);
}
