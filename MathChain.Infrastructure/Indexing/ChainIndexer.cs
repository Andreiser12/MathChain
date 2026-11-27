using MathChain.Domain.Entities;
using MathChain.Domain.Gateways;
using MathChain.Domain.Interfaces;
using MathChain.Infrastructure.Ipfs;
using Microsoft.Extensions.Logging;

namespace MathChain.Infrastructure.Indexing
{
    public enum IndexerStep
    {
        Disabled,
        UpToDate,
        Indexed,
        RangeReduced
    }

    public sealed record IndexerRunResult(IndexerStep Step, long? LastBlock, int Events);

    public class ChainIndexer
    {
        public const string CheckpointId = "main";

        private readonly IChainLogReader _reader;
        private readonly IChainIndexGateway _gateway;
        private readonly ChainIndexerSettings _settings;
        private readonly ILogger<ChainIndexer> _logger;
        private readonly TimeProvider _timeProvider;

        public ChainIndexer(IChainLogReader reader, IChainIndexGateway gateway, ChainIndexerSettings settings, ILogger<ChainIndexer> logger,
            TimeProvider? timeProvider = null)
        {
            _reader = reader;
            _gateway = gateway;
            _settings = settings;
            _logger = logger;
            _timeProvider = timeProvider ?? TimeProvider.System;
            CurrentRange = Math.Max(1, settings.BlockRange);
        }

        public int CurrentRange { get; private set; }

        public void UseRange(int range)
        {
            CurrentRange = Math.Max(1, Math.Min(range, Math.Max(1, _settings.BlockRange)));
        }

        public bool IsEnabled => _settings.Enabled && _settings.StartBlock > 0 && _reader.IsConfigured;

        public async Task<IndexerRunResult> RunOnceAsync(CancellationToken cancellationToken)
        {
            if (!IsEnabled)
            {
                return new IndexerRunResult(IndexerStep.Disabled, null, 0);
            }

            var checkpoint = await _gateway.GetCheckpointAsync(CheckpointId);

            if (checkpoint != null && checkpoint.ChainId != 0 && checkpoint.ChainId != _reader.ChainId)
            {
                throw new InvalidOperationException($"The index was built for chain {checkpoint.ChainId}, but the server is configured for chain {_reader.ChainId}.");
            }

            var next = Math.Max(checkpoint?.LastBlock + 1 ?? _settings.StartBlock, _settings.StartBlock);
            var safeHead = await _reader.GetHeadAsync(cancellationToken) - Math.Max(0, _settings.Confirmations);

            if (next > safeHead)
            {
                return new IndexerRunResult(IndexerStep.UpToDate, checkpoint?.LastBlock, 0);
            }

            var windows = new List<(long From, long To)>();

            for (var from = next; from <= safeHead && windows.Count < Math.Max(1, _settings.Parallelism); from += CurrentRange)
            {
                windows.Add((from, Math.Min(from + CurrentRange - 1, safeHead)));
            }

            IReadOnlyList<ChainLogEvent>[] results;

            try
            {
                results = await Task.WhenAll(windows.Select(w => _reader.GetLogsAsync(w.From, w.To, cancellationToken)));
            }
            catch (BlockRangeTooLargeException exception)
            {
                if (CurrentRange == 1)
                {
                    throw;
                }

                CurrentRange = Math.Max(1, CurrentRange / 2);
                _logger.LogWarning("The RPC node refused a range of blocks ({Message}); using {Range} blocks per request", exception.Message, CurrentRange);
                return new IndexerRunResult(IndexerStep.RangeReduced, checkpoint?.LastBlock, 0);
            }

            var total = 0;
            long last = checkpoint?.LastBlock ?? 0;

            for (var i = 0; i < windows.Count; i++)
            {
                var indexed = new List<IndexedChainEvent>();
                var times = new Dictionary<long, DateTime>();

                foreach (var log in results[i])
                {
                    if (!times.TryGetValue(log.BlockNumber, out var time))
                    {
                        time = await _reader.GetBlockTimeAsync(log.BlockNumber, cancellationToken);
                        times[log.BlockNumber] = time;
                    }

                    var cid = log is CurriculumPublishedLog published && ContentId.TryParseBinary(published.Cid, out var parsed) ? parsed!.ToString() : null;
                    indexed.Add(new IndexedChainEvent(log, time, cid));
                }

                await _gateway.ApplyAsync(CheckpointId, _reader.ChainId, indexed, windows[i].To, _timeProvider.GetUtcNow().UtcDateTime);
                total += indexed.Count;
                last = windows[i].To;
            }

            if (total > 0)
            {
                _logger.LogInformation("Indexed {Events} blockchain events up to block {Block}", total, last);
            }

            return new IndexerRunResult(IndexerStep.Indexed, last, total);
        }

        public async Task<ChainIndexerStatus> GetStatusAsync(CancellationToken cancellationToken)
        {
            var checkpoint = await _gateway.GetCheckpointAsync(CheckpointId);
            long? head = null;

            if (IsEnabled)
            {
                try
                {
                    head = await _reader.GetHeadAsync(cancellationToken);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    _logger.LogWarning(exception, "The chain head could not be read");
                }
            }

            var confirmations = Math.Max(0, _settings.Confirmations);

            return new ChainIndexerStatus
            {
                Enabled = IsEnabled,
                ChainId = _reader.ChainId,
                StartBlock = _settings.StartBlock,
                LastIndexedBlock = checkpoint?.LastBlock,
                ChainHead = head,
                Confirmations = confirmations,
                ConfirmedHead = head.HasValue ? Math.Max(0, head.Value - confirmations) : null,
                UpdatedAt = checkpoint?.UpdatedAt
            };
        }
    }
}
