namespace MathChain.Domain.Entities
{
    public class CurriculumAnchor
    {
        public long Version { get; set; }
        public byte[] Cid { get; set; } = Array.Empty<byte>();
        public DateTime PublishedAt { get; set; }
        public string Publisher { get; set; } = string.Empty;
        public string? TransactionHash { get; set; }
    }

    public class CurriculumRegistryInfo
    {
        public string NetworkName { get; set; } = string.Empty;
        public long ChainId { get; set; }
        public string ExplorerUrl { get; set; } = string.Empty;
        public string ContractAddress { get; set; } = string.Empty;
        public string? PublisherAddress { get; set; }
    }
}
