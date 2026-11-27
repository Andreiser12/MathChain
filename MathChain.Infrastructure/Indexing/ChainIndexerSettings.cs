namespace MathChain.Infrastructure.Indexing
{
    public class ChainIndexerSettings
    {
        public bool Enabled { get; set; } = true;
        public long StartBlock { get; set; }
        public int BlockRange { get; set; } = 200;
        public int Confirmations { get; set; } = 12;
        public int Parallelism { get; set; } = 4;
        public int PollIntervalSeconds { get; set; } = 15;
        public int MaximumRetryDelaySeconds { get; set; } = 120;
    }
}
