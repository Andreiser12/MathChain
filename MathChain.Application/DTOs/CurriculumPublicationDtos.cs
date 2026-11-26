namespace MathChain.Application.DTOs
{
    public class CurriculumAnchorDto
    {
        public long Version { get; set; }
        public string Cid { get; set; } = string.Empty;
        public DateTime PublishedAt { get; set; }
        public string Publisher { get; set; } = string.Empty;
        public string? TransactionHash { get; set; }
        public string? TransactionUrl { get; set; }
        public string ManifestPath { get; set; } = string.Empty;
    }

    public class CurriculumPublicationStatusDto
    {
        public bool Configured { get; set; }
        public bool CanPublish { get; set; }
        public string NetworkName { get; set; } = string.Empty;
        public long ChainId { get; set; }
        public string ContractAddress { get; set; } = string.Empty;
        public string? ContractUrl { get; set; }
        public string? PublisherAddress { get; set; }
        public string CurrentCid { get; set; } = string.Empty;
        public int ChapterCount { get; set; }
        public int FormulaCount { get; set; }
        public CurriculumAnchorDto? Published { get; set; }
        public bool? MatchesPublished { get; set; }
        public string? ChainError { get; set; }
    }
}
