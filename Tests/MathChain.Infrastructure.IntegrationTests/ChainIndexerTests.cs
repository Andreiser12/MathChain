using MathChain.Domain.Entities;
using MathChain.Domain.Enums;
using MathChain.Gateway.Repositories;
using MathChain.Infrastructure.Indexing;
using MathChain.Infrastructure.Ipfs;
using MathChain.Persistence.Context;
using MathChain.Tests.Shared.Fakes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace MathChain.Infrastructure.IntegrationTests
{
    public class ChainIndexerTests
    {
        private const string Student = "0x3333333333333333333333333333333333333333";
        private const string Issuer = "0x70997970c51812dc3a010c7d01b50e0d17dc79c8";
        private readonly string _database = Guid.NewGuid().ToString();
        private readonly FakeChainLogReader _reader = new();
        private readonly ChainIndexerSettings _settings = new() { StartBlock = 100, BlockRange = 50, Confirmations = 10, Parallelism = 3 };

        private MathChainDbContext Context()
        {
            return new MathChainDbContext(new DbContextOptionsBuilder<MathChainDbContext>().UseInMemoryDatabase(_database).Options);
        }

        private ChainIndexer Indexer(MathChainDbContext context)
        {
            return new ChainIndexer(_reader, new ChainIndexRepository(context), _settings, NullLogger<ChainIndexer>.Instance);
        }

        private async Task<IndexerRunResult> RunAsync()
        {
            await using var context = Context();
            return await Indexer(context).RunOnceAsync(CancellationToken.None);
        }

        private async Task RunUntilUpToDateAsync()
        {
            for (var i = 0; i < 100; i++)
            {
                if ((await RunAsync()).Step == IndexerStep.UpToDate)
                {
                    return;
                }
            }

            throw new InvalidOperationException("The indexer did not catch up.");
        }

        private static CertificateIssuedLog Issued(long tokenId, long block, int logIndex = 0, string holder = Student)
        {
            return new CertificateIssuedLog(tokenId, holder, Issuer, CertificateKind.Chapter, "0x" + new string('1', 64), "0x" + new string('2', 64))
            {
                BlockNumber = block,
                LogIndex = logIndex,
                TransactionHash = $"0x{block:x64}"
            };
        }

        [Fact]
        public async Task RunOnceAsync_ShouldReportDisabled_WhenThereIsNoStartBlock()
        {
            _settings.StartBlock = 0;

            Assert.Equal(IndexerStep.Disabled, (await RunAsync()).Step);
            Assert.Empty(_reader.Requests);
        }

        [Fact]
        public async Task RunOnceAsync_ShouldReportDisabled_WhenTheReaderIsNotConfigured()
        {
            _reader.IsConfigured = false;

            Assert.Equal(IndexerStep.Disabled, (await RunAsync()).Step);
        }

        [Fact]
        public async Task RunOnceAsync_ShouldReadParallelWindowsFromTheStartBlockAndSaveTheCheckpoint_WhenItRunsForTheFirstTime()
        {
            var result = await RunAsync();

            Assert.Equal(IndexerStep.Indexed, result.Step);
            Assert.Equal(new[] { (100L, 149L), (150L, 199L), (200L, 249L) }, _reader.Requests.OrderBy(r => r.From));
            await using var context = Context();
            Assert.Equal(249, (await context.IndexerCheckpoints.SingleAsync()).LastBlock);
            Assert.Equal(84532, (await context.IndexerCheckpoints.SingleAsync()).ChainId);
        }

        [Fact]
        public async Task RunOnceAsync_ShouldStopBeforeTheUnconfirmedBlocks_WhenTheHeadIsClose()
        {
            _reader.Head = 130;

            await RunAsync();

            Assert.Equal(new[] { (100L, 120L) }, _reader.Requests);
            Assert.Equal(IndexerStep.UpToDate, (await RunAsync()).Step);
        }

        [Fact]
        public async Task RunOnceAsync_ShouldApplyTheEventsInOrder_WhenACertificateLifecycleSpansSeveralWindows()
        {
            _reader.Head = 1000;
            _reader.Logs.Add(Issued(7, 120));
            _reader.Logs.Add(new CertificateClaimedLog(7, Student) { BlockNumber = 120, LogIndex = 1, TransactionHash = $"0x{120:x64}" });
            _reader.Logs.Add(new CertificateRevokedLog(7, Issuer) { BlockNumber = 480, LogIndex = 0, TransactionHash = $"0x{480:x64}" });

            await RunUntilUpToDateAsync();

            await using var context = Context();
            var certificate = await context.ChainCertificates.SingleAsync();
            Assert.True(certificate.ClaimedBySmartAccount);
            Assert.True(certificate.Revoked);
            Assert.Equal(Issuer, certificate.RevokedBy);
            Assert.Equal(120, certificate.IssuedBlock);
            Assert.Equal(new DateTime(2026, 11, 26, 0, 0, 0, DateTimeKind.Utc).AddSeconds(240), certificate.IssuedAt);
            Assert.Equal(990, (await context.IndexerCheckpoints.SingleAsync()).LastBlock);
        }

        [Fact]
        public async Task RunOnceAsync_ShouldNotDuplicateAnything_WhenTheSameBlocksAreReprocessed()
        {
            _reader.Head = 300;
            _reader.Logs.Add(Issued(1, 110));
            _reader.Logs.Add(new SolutionPurchasedLog(Student, "0x" + new string('a', 64), Student, 1, 1000000) { BlockNumber = 150, LogIndex = 3, TransactionHash = $"0x{150:x64}" });
            _reader.Logs.Add(new AccountCreatedLog("0x4444444444444444444444444444444444444444", Student) { BlockNumber = 160, TransactionHash = $"0x{160:x64}" });
            _reader.Logs.Add(new PriceChangedLog(2, 2000000) { BlockNumber = 170, TransactionHash = $"0x{170:x64}" });

            await RunUntilUpToDateAsync();
            await using (var context = Context())
            {
                context.IndexerCheckpoints.RemoveRange(context.IndexerCheckpoints);
                await context.SaveChangesAsync();
            }
            await RunUntilUpToDateAsync();

            await using var check = Context();
            Assert.Equal(1, await check.ChainCertificates.CountAsync());
            Assert.Equal(1, await check.ChainSolutionPurchases.CountAsync());
            Assert.Equal(1, await check.ChainSmartAccounts.CountAsync());
            Assert.Equal(1, await check.ChainPriceChanges.CountAsync());
            Assert.Equal(1000000m, (await check.ChainSolutionPurchases.SingleAsync()).Price);
        }

        [Fact]
        public async Task RunOnceAsync_ShouldHalveTheWindowAndContinue_WhenTheRangeIsRefused()
        {
            _reader.Head = 400;
            _reader.MaximumRange = 20;

            Assert.Equal(IndexerStep.RangeReduced, (await RunAsync()).Step);
            await using var context = Context();
            var indexer = Indexer(context);
            indexer.UseRange(25);
            Assert.Equal(IndexerStep.RangeReduced, (await indexer.RunOnceAsync(CancellationToken.None)).Step);
            Assert.Equal(12, indexer.CurrentRange);
            Assert.Equal(IndexerStep.Indexed, (await indexer.RunOnceAsync(CancellationToken.None)).Step);
            Assert.Equal(135, (await context.IndexerCheckpoints.AsNoTracking().SingleAsync()).LastBlock);
        }

        [Fact]
        public async Task RunOnceAsync_ShouldThrow_WhenTheIndexWasBuiltForAnotherChain()
        {
            await RunAsync();
            _reader.ChainId = 1;

            await Assert.ThrowsAsync<InvalidOperationException>(RunAsync);
        }

        [Fact]
        public async Task RunOnceAsync_ShouldStoreTheTextCid_WhenTheCurriculumIsPublished()
        {
            var cid = ContentId.ForBlock(ContentCodec.Raw, new byte[] { 1, 2, 3 });
            _reader.Head = 200;
            _reader.Logs.Add(new CurriculumPublishedLog(1, cid.ToBytes(), Issuer) { BlockNumber = 130, TransactionHash = $"0x{130:x64}" });

            await RunUntilUpToDateAsync();

            await using var context = Context();
            Assert.Equal(cid.ToString(), (await context.ChainCurriculumPublications.SingleAsync()).Cid);
        }

        [Fact]
        public async Task RunOnceAsync_ShouldReadEachBlockTimeOnce_WhenABlockHasSeveralEvents()
        {
            _reader.Head = 200;
            _reader.Logs.Add(Issued(1, 110, 0));
            _reader.Logs.Add(Issued(2, 110, 1, "0x5555555555555555555555555555555555555555"));
            _reader.Logs.Add(Issued(3, 111, 0));

            await RunUntilUpToDateAsync();

            Assert.Equal(new long[] { 110, 111 }, _reader.TimeRequests);
        }

        [Fact]
        public async Task GetStatsAsync_ShouldSummariseTheIndex_WhenItHasCertificatesAndPurchases()
        {
            _reader.Head = 300;
            _reader.Logs.Add(Issued(1, 110));
            _reader.Logs.Add(Issued(2, 111));
            _reader.Logs.Add(new CertificateRevokedLog(2, Issuer) { BlockNumber = 112, TransactionHash = $"0x{112:x64}" });
            _reader.Logs.Add(new SolutionPurchasedLog(Student, "0x" + new string('a', 64), Student, 0, 1000000) { BlockNumber = 113, TransactionHash = $"0x{113:x64}" });
            _reader.Logs.Add(new SolutionPurchasedLog(Student, "0x" + new string('b', 64), Student, 2, 3000000) { BlockNumber = 114, TransactionHash = $"0x{114:x64}" });

            await RunUntilUpToDateAsync();

            await using var context = Context();
            var stats = await new ChainIndexRepository(context).GetStatsAsync();
            Assert.Equal((2, 1, 1, 2, 4000000m), (stats.Certificates, stats.ActiveCertificates, stats.RevokedCertificates, stats.SolutionPurchases, stats.SolutionRevenue));
        }

        [Fact]
        public async Task GetStatusAsync_ShouldReportTheConfirmedHead_WhenTheIndexIsUpToDate()
        {
            _reader.Head = 300;

            await RunUntilUpToDateAsync();

            await using var context = Context();
            var status = await Indexer(context).GetStatusAsync(CancellationToken.None);
            Assert.Equal((300L, 10, 290L, 290L), (status.ChainHead!.Value, status.Confirmations, status.ConfirmedHead!.Value, status.LastIndexedBlock!.Value));
        }
    }
}
