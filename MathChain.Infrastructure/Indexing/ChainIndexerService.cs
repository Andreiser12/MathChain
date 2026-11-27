using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MathChain.Infrastructure.Indexing
{
    public class ChainIndexerService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopes;
        private readonly ChainIndexerSettings _settings;
        private readonly ILogger<ChainIndexerService> _logger;

        public ChainIndexerService(IServiceScopeFactory scopes, ChainIndexerSettings settings, ILogger<ChainIndexerService> logger)
        {
            _scopes = scopes;
            _settings = settings;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var failures = 0;
            var range = Math.Max(1, _settings.BlockRange);

            while (!stoppingToken.IsCancellationRequested)
            {
                TimeSpan delay;

                try
                {
                    using var scope = _scopes.CreateScope();
                    var indexer = scope.ServiceProvider.GetRequiredService<ChainIndexer>();
                    indexer.UseRange(range);
                    var result = await indexer.RunOnceAsync(stoppingToken);
                    range = indexer.CurrentRange;
                    failures = 0;

                    if (result.Step == IndexerStep.Disabled)
                    {
                        _logger.LogInformation("The blockchain indexer is disabled. Set Indexer:StartBlock and the contract addresses to enable it.");
                        return;
                    }

                    delay = result.Step == IndexerStep.UpToDate ? TimeSpan.FromSeconds(Math.Max(1, _settings.PollIntervalSeconds)) : TimeSpan.Zero;
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception exception)
                {
                    failures++;
                    delay = TimeSpan.FromSeconds(Math.Min(_settings.MaximumRetryDelaySeconds, Math.Pow(2, Math.Min(failures, 10))));
                    _logger.LogWarning(exception, "The blockchain indexer failed; retrying in {Delay}", delay);
                }

                if (delay > TimeSpan.Zero)
                {
                    try
                    {
                        await Task.Delay(delay, stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                }
            }
        }
    }
}
