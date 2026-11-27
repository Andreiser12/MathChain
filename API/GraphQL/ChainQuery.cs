using HotChocolate;
using System.Linq.Expressions;
using HotChocolate.Data;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using MathChain.Domain.Entities;
using MathChain.Domain.Gateways;
using MathChain.Infrastructure.Indexing;

namespace MathChain.API.GraphQL
{
    public class ChainQuery
    {
        private const int MaximumPageSize = 100;
        private const int DefaultPageSize = 20;

        [UseOffsetPaging(MaxPageSize = MaximumPageSize, DefaultPageSize = DefaultPageSize, IncludeTotalCount = true)]
        [UseFiltering]
        [UseSorting]
        public IQueryable<ChainCertificate> GetCertificates([Service] IChainIndexGateway index, IResolverContext context)
        {
            return DefaultOrder(context, index.Certificates, c => c.TokenId);
        }

        public Task<ChainCertificate?> GetCertificate(long tokenId, [Service] IChainIndexGateway index)
        {
            return index.FindCertificateAsync(tokenId);
        }

        [UseOffsetPaging(MaxPageSize = MaximumPageSize, DefaultPageSize = DefaultPageSize, IncludeTotalCount = true)]
        [UseFiltering]
        [UseSorting]
        public IQueryable<ChainSmartAccount> GetSmartAccounts([Service] IChainIndexGateway index, IResolverContext context)
        {
            return DefaultOrder(context, index.SmartAccounts, a => a.CreatedBlock);
        }

        [UseOffsetPaging(MaxPageSize = MaximumPageSize, DefaultPageSize = DefaultPageSize, IncludeTotalCount = true)]
        [UseFiltering]
        [UseSorting]
        public IQueryable<ChainSolutionPurchase> GetSolutionPurchases([Service] IChainIndexGateway index, IResolverContext context)
        {
            return DefaultOrder(context, index.SolutionPurchases, p => p.Block);
        }

        [UseOffsetPaging(MaxPageSize = MaximumPageSize, DefaultPageSize = DefaultPageSize, IncludeTotalCount = true)]
        [UseFiltering]
        [UseSorting]
        public IQueryable<ChainPriceChange> GetPriceChanges([Service] IChainIndexGateway index, IResolverContext context)
        {
            return DefaultOrder(context, index.PriceChanges, p => p.Block);
        }

        [UseOffsetPaging(MaxPageSize = MaximumPageSize, DefaultPageSize = DefaultPageSize, IncludeTotalCount = true)]
        [UseFiltering]
        [UseSorting]
        public IQueryable<ChainCurriculumPublication> GetCurriculumPublications([Service] IChainIndexGateway index, IResolverContext context)
        {
            return DefaultOrder(context, index.CurriculumPublications, p => p.Version);
        }

        public Task<ChainIndexStats> GetStats([Service] IChainIndexGateway index)
        {
            return index.GetStatsAsync();
        }

        public Task<ChainIndexerStatus> GetIndexerStatus([Service] ChainIndexer indexer, CancellationToken cancellationToken)
        {
            return indexer.GetStatusAsync(cancellationToken);
        }

        private static IQueryable<T> DefaultOrder<T, TKey>(IResolverContext context, IQueryable<T> source, Expression<Func<T, TKey>> key)
        {
            var order = context.ArgumentKind("order");

            return order is ValueKind.List or ValueKind.Object ? source : source.OrderBy(key);
        }
    }
}
