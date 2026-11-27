using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MathChain.Domain.Entities;
using MathChain.Domain.Enums;

namespace MathChain.API.IntegrationTests
{
    public class GraphQLEndpointTests : IClassFixture<ApiFactory>
    {
        private const string Holder = "0x3333333333333333333333333333333333333333";
        private readonly ApiFactory _factory;

        public GraphQLEndpointTests(ApiFactory factory)
        {
            _factory = factory;

            if (_factory.ChainIndex.CertificateRows.Count == 0)
            {
                for (var tokenId = 1; tokenId <= 150; tokenId++)
                {
                    _factory.ChainIndex.CertificateRows.Add(new ChainCertificate
                    {
                        TokenId = tokenId,
                        Holder = tokenId % 3 == 0 ? Holder : "0x4444444444444444444444444444444444444444",
                        Issuer = "0x70997970c51812dc3a010c7d01b50e0d17dc79c8",
                        Kind = tokenId % 2 == 0 ? CertificateKind.Chapter : CertificateKind.Class,
                        SubjectId = "0x" + new string('1', 64),
                        DataHash = "0x" + new string('2', 64),
                        IssuedBlock = 63000000 + tokenId,
                        IssuedAt = new DateTime(2026, 11, 20, 0, 0, 0, DateTimeKind.Utc).AddMinutes(tokenId),
                        IssuedTransaction = "0x" + tokenId.ToString("x64"),
                        Revoked = tokenId == 9,
                        RevokedTransaction = tokenId == 9 ? "0x" + new string('f', 64) : null
                    });
                }

                _factory.ChainIndex.PurchaseRows.Add(new ChainSolutionPurchase { Id = "p1", Account = Holder, Payer = Holder, SolutionId = "0x01", Tier = 2, Price = 3000000, Transaction = "0x01" });
                _factory.ChainIndex.PublicationRows.Add(new ChainCurriculumPublication { Version = 1, Cid = "bafkreiexample", Publisher = Holder, Transaction = "0x02" });
            }
        }

        private async Task<JsonElement> QueryAsync(string query, object? variables = null, HttpStatusCode expected = HttpStatusCode.OK)
        {
            var response = await _factory.CreateClient().PostAsJsonAsync("/api/graphql", new { query, variables }, TestContext.Current.CancellationToken);
            Assert.Equal(expected, response.StatusCode);
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
            return document.RootElement.Clone();
        }

        [Fact]
        public async Task Certificates_ShouldSupportFilteringSortingAndPaging_WhenTheUserIsAnonymous()
        {
            var result = await QueryAsync("""
                query ($holder: String!) {
                  certificates(skip: 2, take: 5, where: { holder: { eq: $holder }, revoked: { eq: false } }, order: [{ tokenId: DESC }]) {
                    totalCount
                    items { tokenId holder kind revoked }
                    pageInfo { hasNextPage }
                  }
                }
                """, new { holder = Holder });

            var page = result.GetProperty("data").GetProperty("certificates");
            Assert.Equal(49, page.GetProperty("totalCount").GetInt32());
            Assert.Equal(new long[] { 144, 141, 138, 135, 132 }, page.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("tokenId").GetInt64()));
            Assert.True(page.GetProperty("pageInfo").GetProperty("hasNextPage").GetBoolean());
            Assert.Equal("CHAPTER", page.GetProperty("items")[0].GetProperty("kind").GetString());
        }

        [Fact]
        public async Task Certificates_ShouldBeSortedByTokenId_WhenNoOrderIsGiven()
        {
            var result = await QueryAsync("{ certificates(take: 3) { items { tokenId } } }");

            Assert.Equal(new long[] { 1, 2, 3 }, result.GetProperty("data").GetProperty("certificates").GetProperty("items").EnumerateArray().Select(i => i.GetProperty("tokenId").GetInt64()));
        }

        [Fact]
        public async Task Certificates_ShouldUseTheRequestedOrder_WhenOrderedByIssueDate()
        {
            var result = await QueryAsync("{ certificates(take: 2, order: { issuedAt: DESC }) { items { tokenId } } }");

            Assert.Equal(new long[] { 150, 149 }, result.GetProperty("data").GetProperty("certificates").GetProperty("items").EnumerateArray().Select(i => i.GetProperty("tokenId").GetInt64()));
        }

        [Fact]
        public async Task Certificate_ShouldReturnTheRevocation_WhenQueriedByTokenId()
        {
            var result = await QueryAsync("{ certificate(tokenId: 9) { tokenId revoked revokedTransaction issuedBlock } missing: certificate(tokenId: 999) { tokenId } }");

            var data = result.GetProperty("data");
            Assert.True(data.GetProperty("certificate").GetProperty("revoked").GetBoolean());
            Assert.Equal(63000009, data.GetProperty("certificate").GetProperty("issuedBlock").GetInt64());
            Assert.Equal(JsonValueKind.Null, data.GetProperty("missing").ValueKind);
        }

        [Fact]
        public async Task Certificates_ShouldBeRejected_WhenMoreThanTheMaximumPageSizeIsRequested()
        {
            var result = await QueryAsync("{ certificates(take: 500) { items { tokenId } } }");

            Assert.True(result.TryGetProperty("errors", out var errors) && errors.GetArrayLength() > 0);
        }

        [Fact]
        public async Task StatsAndIndexerStatus_ShouldSummariseTheIndex_WhenTheIndexHasData()
        {
            var result = await QueryAsync("{ stats { certificates revokedCertificates solutionPurchases solutionRevenue curriculumVersions } indexerStatus { enabled lastIndexedBlock } }");

            var stats = result.GetProperty("data").GetProperty("stats");
            Assert.Equal(150, stats.GetProperty("certificates").GetInt32());
            Assert.Equal(1, stats.GetProperty("revokedCertificates").GetInt32());
            Assert.Equal(3000000m, stats.GetProperty("solutionRevenue").GetDecimal());
            Assert.Equal(1, stats.GetProperty("curriculumVersions").GetInt32());
            Assert.False(result.GetProperty("data").GetProperty("indexerStatus").GetProperty("enabled").GetBoolean());
        }

        [Fact]
        public async Task Mutations_ShouldBeRejected_WhenTheClientSendsOne()
        {
            var result = await QueryAsync("mutation { revoke(tokenId: 1) }", expected: HttpStatusCode.BadRequest);

            Assert.True(result.TryGetProperty("errors", out _));
        }

        [Fact]
        public async Task Introspection_ShouldDescribeTheSchema_WhenATypeIsQueried()
        {
            var result = await QueryAsync("{ __type(name: \"ChainCertificate\") { fields { name } } }");

            var fields = result.GetProperty("data").GetProperty("__type").GetProperty("fields").EnumerateArray().Select(f => f.GetProperty("name").GetString()).ToList();
            Assert.Contains("holder", fields);
            Assert.Contains("claimedBySmartAccount", fields);
        }
    }
}
