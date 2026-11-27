using MathChain.Domain.Entities;
using MathChain.Domain.Interfaces;

namespace MathChain.Tests.Fakes
{
    public class FakeChainLogReader : IChainLogReader
    {
        public List<ChainLogEvent> Logs { get; } = new();
        public List<(long From, long To)> Requests { get; } = new();
        public List<long> TimeRequests { get; } = new();
        public long Head { get; set; } = 1000;
        public int? MaximumRange { get; set; }
        public bool IsConfigured { get; set; } = true;
        public long ChainId { get; set; } = 84532;

        public Task<long> GetHeadAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(Head);
        }

        public Task<IReadOnlyList<ChainLogEvent>> GetLogsAsync(long fromBlock, long toBlock, CancellationToken cancellationToken)
        {
            lock (Requests)
            {
                Requests.Add((fromBlock, toBlock));
            }

            if (MaximumRange.HasValue && toBlock - fromBlock + 1 > MaximumRange.Value)
            {
                throw new BlockRangeTooLargeException($"eth_getLogs is limited to a {MaximumRange} range");
            }

            return Task.FromResult<IReadOnlyList<ChainLogEvent>>(Logs.Where(l => l.BlockNumber >= fromBlock && l.BlockNumber <= toBlock)
                .OrderBy(l => l.BlockNumber).ThenBy(l => l.LogIndex).ToList());
        }

        public Task<DateTime> GetBlockTimeAsync(long block, CancellationToken cancellationToken)
        {
            TimeRequests.Add(block);
            return Task.FromResult(new DateTime(2026, 11, 26, 0, 0, 0, DateTimeKind.Utc).AddSeconds(block * 2));
        }
    }
}
