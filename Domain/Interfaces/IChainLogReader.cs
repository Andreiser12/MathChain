using MathChain.Domain.Entities;

namespace MathChain.Domain.Interfaces
{
    public interface IChainLogReader
    {
        bool IsConfigured { get; }
        long ChainId { get; }
        Task<long> GetHeadAsync(CancellationToken cancellationToken);
        Task<IReadOnlyList<ChainLogEvent>> GetLogsAsync(long fromBlock, long toBlock, CancellationToken cancellationToken);
        Task<DateTime> GetBlockTimeAsync(long block, CancellationToken cancellationToken);
    }

    public sealed class BlockRangeTooLargeException : Exception
    {
        public BlockRangeTooLargeException(string message, Exception? inner = null) : base(message, inner)
        {
        }
    }
}
