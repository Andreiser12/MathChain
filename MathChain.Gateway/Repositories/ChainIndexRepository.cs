using MathChain.Domain.Entities;
using MathChain.Domain.Gateways;
using MathChain.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace MathChain.Gateway.Repositories
{
    public class ChainIndexRepository : BaseRepository, IChainIndexGateway
    {
        public ChainIndexRepository(MathChainDbContext context) : base(context)
        {
        }

        public IQueryable<ChainCertificate> Certificates => _context.ChainCertificates.AsNoTracking();
        public IQueryable<ChainSmartAccount> SmartAccounts => _context.ChainSmartAccounts.AsNoTracking();
        public IQueryable<ChainSolutionPurchase> SolutionPurchases => _context.ChainSolutionPurchases.AsNoTracking();
        public IQueryable<ChainPriceChange> PriceChanges => _context.ChainPriceChanges.AsNoTracking();
        public IQueryable<ChainCurriculumPublication> CurriculumPublications => _context.ChainCurriculumPublications.AsNoTracking();

        public async Task<IndexerCheckpoint?> GetCheckpointAsync(string id)
        {
            return await _context.IndexerCheckpoints.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task ApplyAsync(string checkpointId, long chainId, IReadOnlyList<IndexedChainEvent> events, long lastBlock, DateTime now)
        {
            var tokenIds = events.Select(e => e.Event switch
            {
                CertificateIssuedLog issued => issued.TokenId,
                CertificateRevokedLog revoked => revoked.TokenId,
                CertificateClaimedLog claimed => claimed.TokenId,
                _ => (long?)null
            }).Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
            var certificates = await _context.ChainCertificates.Where(c => tokenIds.Contains(c.TokenId)).ToDictionaryAsync(c => c.TokenId);
            var eventIds = events.Select(e => e.Event.Id).ToList();
            var purchaseIds = (await _context.ChainSolutionPurchases.Where(p => eventIds.Contains(p.Id)).Select(p => p.Id).ToListAsync()).ToHashSet();
            var priceIds = (await _context.ChainPriceChanges.Where(p => eventIds.Contains(p.Id)).Select(p => p.Id).ToListAsync()).ToHashSet();
            var accounts = events.Select(e => e.Event).OfType<AccountCreatedLog>().Select(a => a.Account).ToList();
            var knownAccounts = (await _context.ChainSmartAccounts.Where(a => accounts.Contains(a.Account)).Select(a => a.Account).ToListAsync()).ToHashSet();
            var versions = events.Select(e => e.Event).OfType<CurriculumPublishedLog>().Select(p => p.Version).ToList();
            var knownVersions = (await _context.ChainCurriculumPublications.Where(p => versions.Contains(p.Version)).Select(p => p.Version).ToListAsync()).ToHashSet();

            foreach (var indexed in events.OrderBy(e => e.Event.BlockNumber).ThenBy(e => e.Event.LogIndex))
            {
                var log = indexed.Event;

                switch (log)
                {
                    case CertificateIssuedLog issued when !certificates.ContainsKey(issued.TokenId):
                        var certificate = new ChainCertificate
                        {
                            TokenId = issued.TokenId,
                            Holder = issued.Student,
                            Issuer = issued.Issuer,
                            Kind = issued.Kind,
                            SubjectId = issued.SubjectId,
                            DataHash = issued.DataHash,
                            IssuedBlock = issued.BlockNumber,
                            IssuedAt = indexed.BlockTime,
                            IssuedTransaction = issued.TransactionHash
                        };
                        certificates[issued.TokenId] = certificate;
                        _context.ChainCertificates.Add(certificate);
                        break;
                    case CertificateRevokedLog revoked when certificates.TryGetValue(revoked.TokenId, out var target) && !target.Revoked:
                        target.Revoked = true;
                        target.RevokedBy = revoked.RevokedBy;
                        target.RevokedAt = indexed.BlockTime;
                        target.RevokedTransaction = revoked.TransactionHash;
                        break;
                    case CertificateClaimedLog claimed when certificates.TryGetValue(claimed.TokenId, out var claimedCertificate):
                        claimedCertificate.ClaimedBySmartAccount = true;
                        break;
                    case AccountCreatedLog created when knownAccounts.Add(created.Account):
                        _context.ChainSmartAccounts.Add(new ChainSmartAccount
                        {
                            Account = created.Account,
                            Owner = created.Owner,
                            CreatedBlock = created.BlockNumber,
                            CreatedAt = indexed.BlockTime,
                            Transaction = created.TransactionHash
                        });
                        break;
                    case SolutionPurchasedLog purchased when purchaseIds.Add(purchased.Id):
                        _context.ChainSolutionPurchases.Add(new ChainSolutionPurchase
                        {
                            Id = purchased.Id,
                            Account = purchased.Account,
                            SolutionId = purchased.SolutionId,
                            Payer = purchased.Payer,
                            Tier = purchased.Tier,
                            Price = purchased.Price,
                            Block = purchased.BlockNumber,
                            PurchasedAt = indexed.BlockTime,
                            Transaction = purchased.TransactionHash
                        });
                        break;
                    case PriceChangedLog changed when priceIds.Add(changed.Id):
                        _context.ChainPriceChanges.Add(new ChainPriceChange
                        {
                            Id = changed.Id,
                            Tier = changed.Tier,
                            Price = changed.Price,
                            Block = changed.BlockNumber,
                            ChangedAt = indexed.BlockTime,
                            Transaction = changed.TransactionHash
                        });
                        break;
                    case CurriculumPublishedLog published when knownVersions.Add(published.Version):
                        _context.ChainCurriculumPublications.Add(new ChainCurriculumPublication
                        {
                            Version = published.Version,
                            Cid = indexed.Cid ?? Convert.ToHexString(published.Cid).ToLowerInvariant(),
                            Publisher = published.Publisher,
                            PublishedAt = indexed.BlockTime,
                            Block = published.BlockNumber,
                            Transaction = published.TransactionHash
                        });
                        break;
                }
            }

            var checkpoint = await _context.IndexerCheckpoints.FirstOrDefaultAsync(c => c.Id == checkpointId);

            if (checkpoint == null)
            {
                checkpoint = new IndexerCheckpoint { Id = checkpointId };
                _context.IndexerCheckpoints.Add(checkpoint);
            }

            checkpoint.ChainId = chainId;
            checkpoint.LastBlock = Math.Max(checkpoint.LastBlock, lastBlock);
            checkpoint.UpdatedAt = now;

            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();
        }

        public async Task<ChainCertificate?> FindCertificateAsync(long tokenId)
        {
            return await _context.ChainCertificates.AsNoTracking().FirstOrDefaultAsync(c => c.TokenId == tokenId);
        }

        public async Task<ChainIndexStats> GetStatsAsync()
        {
            return new ChainIndexStats
            {
                Certificates = await _context.ChainCertificates.CountAsync(),
                ActiveCertificates = await _context.ChainCertificates.CountAsync(c => !c.Revoked),
                RevokedCertificates = await _context.ChainCertificates.CountAsync(c => c.Revoked),
                CertificatesClaimedBySmartAccounts = await _context.ChainCertificates.CountAsync(c => c.ClaimedBySmartAccount),
                SmartAccounts = await _context.ChainSmartAccounts.CountAsync(),
                SolutionPurchases = await _context.ChainSolutionPurchases.CountAsync(),
                SolutionRevenue = await _context.ChainSolutionPurchases.SumAsync(p => (decimal?)p.Price) ?? 0,
                CurriculumVersions = await _context.ChainCurriculumPublications.CountAsync()
            };
        }
    }
}
