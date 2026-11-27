using System.Text;
using System.Text.Json;
using MathChain.Application.Contexts;
using MathChain.Application.Curriculum;
using MathChain.Domain.Entities;
using MathChain.Domain.Enums;
using MathChain.Domain.Exceptions;
using MathChain.Infrastructure.Ipfs;
using MathChain.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace MathChain.Tests.Application
{
    public class CurriculumPublicationTests
    {
        private const string Admin = "0x6666666666666666666666666666666666666666";

        private readonly InMemoryCurriculumGateway _curriculum = new();
        private readonly InMemoryContentBlockGateway _blocks = new();
        private readonly FakeCurriculumRegistry _registry = new();
        private readonly FakeCurrentUser _currentUser = new();
        private readonly IpfsStorageService _ipfs;
        private readonly CurriculumPublicationContext _context;
        private readonly CurriculumChapter _root;
        private readonly CurriculumChapter _first;
        private readonly CurriculumChapter _second;

        public CurriculumPublicationTests()
        {
            _ipfs = new IpfsStorageService(_blocks, NullLogger<IpfsStorageService>.Instance);
            _context = new CurriculumPublicationContext(_curriculum, _ipfs, _registry, new AccessGuard(_currentUser, new InMemoryClassRoomGateway()));
            _root = Chapter("Algebra", null, 1, "70000000-0000-0000-0000-000000000001");
            _first = Chapter("Puteri", _root, 1, "70000000-0000-0000-0000-000000000002");
            _second = Chapter("Radicali", _root, 2, "70000000-0000-0000-0000-000000000003");
            _curriculum.Formulas.Add(new CurriculumFormula { Id = Guid.Parse("80000000-0000-0000-0000-000000000001"), ChapterId = _first.Id, FormulaCode = "f1", Title = "Produs", Latex = "a^m a^n = a^{m+n}", Order = 1 });
            _curriculum.Formulas.Add(new CurriculumFormula { Id = Guid.Parse("80000000-0000-0000-0000-000000000002"), ChapterId = _first.Id, FormulaCode = "f2", Title = "Cât", Latex = "a^m / a^n = a^{m-n}", Order = 2 });
            _curriculum.Prerequisites.Add(new ChapterPrerequisite { ChapterId = _second.Id, PrerequisiteId = _first.Id });
            _currentUser.As(Admin, UserRole.Admin);
        }

        private CurriculumChapter Chapter(string title, CurriculumChapter? parent, int order, string id)
        {
            var chapter = new CurriculumChapter { Id = Guid.Parse(id), ParentId = parent?.Id, Title = title, ChapterSlug = title.ToLowerInvariant(), Order = order };
            _curriculum.Chapters.Add(chapter);
            return chapter;
        }

        private CurriculumDocuments Build()
        {
            return CurriculumManifest.Build(_curriculum.Chapters, _curriculum.Prerequisites, _curriculum.Formulas, _ipfs.ComputeHash);
        }

        [Fact]
        public void Build_ShouldProduceCanonicalJsonWithSortedKeysAndChapters_WhenTheCurriculumIsBuilt()
        {
            var documents = Build();
            var formulasCid = _ipfs.ComputeHash(documents.ChapterFormulas[_first.Id]);
            var expected = "{\"chapters\":["
                + "{\"formulas\":null,\"id\":\"70000000-0000-0000-0000-000000000001\",\"order\":1,\"parent\":null,\"prerequisites\":[],\"slug\":\"algebra\",\"title\":\"Algebra\"},"
                + $"{{\"formulas\":\"{formulasCid}\",\"id\":\"70000000-0000-0000-0000-000000000002\",\"order\":1,\"parent\":\"70000000-0000-0000-0000-000000000001\",\"prerequisites\":[],\"slug\":\"puteri\",\"title\":\"Puteri\"}},"
                + "{\"formulas\":null,\"id\":\"70000000-0000-0000-0000-000000000003\",\"order\":2,\"parent\":\"70000000-0000-0000-0000-000000000001\",\"prerequisites\":[\"70000000-0000-0000-0000-000000000002\"],\"slug\":\"radicali\",\"title\":\"Radicali\"}"
                + "],\"schema\":\"mathchain.curriculum/v1\"}";

            Assert.Equal(expected, Encoding.UTF8.GetString(documents.Manifest));
            Assert.Single(documents.ChapterFormulas);
            Assert.StartsWith("{\"chapter\":\"70000000-0000-0000-0000-000000000002\",\"formulas\":[{\"code\":\"f1\"", Encoding.UTF8.GetString(documents.ChapterFormulas[_first.Id]));
            Assert.Contains("\"title\":\"Cât\"", Encoding.UTF8.GetString(documents.ChapterFormulas[_first.Id]));
        }

        [Fact]
        public void Build_ShouldGiveTheSameBytes_WhenTheSameCurriculumIsInAnotherOrder()
        {
            var before = Build().Manifest;
            _curriculum.Chapters.Reverse();
            _curriculum.Formulas.Reverse();

            Assert.Equal(before, Build().Manifest);
        }

        [Fact]
        public void Build_ShouldChangeTheManifest_WhenAnythingChanges()
        {
            var original = _ipfs.ComputeHash(Build().Manifest);

            _curriculum.Formulas[0].Latex = "a^m \\cdot a^n = a^{m+n}";
            var formulaChanged = _ipfs.ComputeHash(Build().Manifest);
            _curriculum.Formulas[0].Latex = "a^m a^n = a^{m+n}";
            _curriculum.Prerequisites.Clear();
            var prerequisiteRemoved = _ipfs.ComputeHash(Build().Manifest);
            _curriculum.Prerequisites.Add(new ChapterPrerequisite { ChapterId = _second.Id, PrerequisiteId = _first.Id });
            var restored = _ipfs.ComputeHash(Build().Manifest);

            Assert.NotEqual(original, formulaChanged);
            Assert.NotEqual(original, prerequisiteRemoved);
            Assert.Equal(original, restored);
        }

        [Fact]
        public void Build_ShouldProduceValidJson_WhenTheManifestIsParsed()
        {
            using var document = JsonDocument.Parse(Build().Manifest);

            Assert.Equal(3, document.RootElement.GetProperty("chapters").GetArrayLength());
        }

        [Fact]
        public async Task GetStatusAsync_ShouldShowTheCurrentCid_WhenNothingIsPublished()
        {
            var status = await _context.GetStatusAsync();

            Assert.True(status.Configured);
            Assert.Equal(_ipfs.ComputeHash(Build().Manifest), status.CurrentCid);
            Assert.StartsWith("bafkrei", status.CurrentCid);
            Assert.Null(status.Published);
            Assert.Null(status.MatchesPublished);
            Assert.Equal(3, status.ChapterCount);
            Assert.Equal(2, status.FormulaCount);
            Assert.Empty(_blocks.Blocks);
        }

        [Fact]
        public async Task PublishAsync_ShouldStoreTheDocumentsAndAnchorTheCid_WhenTheCurriculumIsPublished()
        {
            var status = await _context.PublishAsync();

            var anchor = Assert.Single(_registry.Publications);
            Assert.Equal(1, status.Published!.Version);
            Assert.Equal(status.CurrentCid, status.Published.Cid);
            Assert.True(status.MatchesPublished);
            Assert.Equal(ContentId.Parse(status.CurrentCid).ToBytes(), anchor.Cid);
            Assert.Equal($"https://explorer.example/tx/{anchor.TransactionHash}", status.Published.TransactionUrl);
            Assert.Equal(Build().Manifest, await _ipfs.GetFileAsync(status.CurrentCid));
            Assert.Equal(Build().ChapterFormulas[_first.Id], await _ipfs.GetFileAsync(_ipfs.ComputeHash(Build().ChapterFormulas[_first.Id])));
        }

        [Fact]
        public async Task GetStatusAsync_ShouldReportThatTheCurriculumDiffersFromTheChain_WhenItChangedAfterPublishing()
        {
            await _context.PublishAsync();

            Assert.True((await _context.GetStatusAsync()).MatchesPublished);

            _curriculum.Prerequisites.Clear();
            var changed = await _context.GetStatusAsync();

            Assert.False(changed.MatchesPublished);
            Assert.NotEqual(changed.Published!.Cid, changed.CurrentCid);
        }

        [Fact]
        public async Task PublishAsync_ShouldThrowBusinessRuleException_WhenTheCurriculumIsUnchanged()
        {
            await _context.PublishAsync();

            var exception = await Assert.ThrowsAsync<BusinessRuleException>(() => _context.PublishAsync());

            Assert.Contains("version 1", exception.Message);
            Assert.Single(_registry.Publications);
        }

        [Fact]
        public async Task PublishAsync_ShouldCreateANewVersion_WhenTheCurriculumChanged()
        {
            await _context.PublishAsync();
            _curriculum.Formulas.RemoveAt(1);

            var status = await _context.PublishAsync();

            Assert.Equal(2, status.Published!.Version);
        }

        [Fact]
        public async Task PublishAsync_ShouldThrowForbiddenException_WhenTheUserIsNotAnAdmin()
        {
            _currentUser.As("0x1111111111111111111111111111111111111111", UserRole.Teacher);

            await Assert.ThrowsAsync<ForbiddenException>(() => _context.PublishAsync());
            await Assert.ThrowsAsync<ForbiddenException>(() => _context.GetStatusAsync());
            Assert.Empty(_registry.Publications);
        }

        [Fact]
        public async Task PublishAsync_ShouldThrowServiceUnavailableException_WhenTheRegistryHasNoKey()
        {
            _registry.CanPublish = false;

            await Assert.ThrowsAsync<ServiceUnavailableException>(() => _context.PublishAsync());
        }

        [Fact]
        public async Task GetStatusAsync_ShouldStillShowTheCurrentCid_WhenTheChainIsUnreachable()
        {
            _registry.Unreachable = true;

            var status = await _context.GetStatusAsync();

            Assert.NotEmpty(status.CurrentCid);
            Assert.Contains("could not be reached", status.ChainError);
        }

        [Fact]
        public async Task GetLatestAsync_ShouldReturnTheLatestVersion_WhenTheUserIsAVisitor()
        {
            await _context.PublishAsync();
            _currentUser.Anonymous();

            var latest = await _context.GetLatestAsync();

            Assert.Equal(1, latest!.Version);
            Assert.Equal($"/api/ipfs/{latest.Cid}?fileName=curriculum-v1.json", latest.ManifestPath);
        }

        [Fact]
        public async Task GetLatestAsync_ShouldReturnNothing_WhenTheRegistryIsNotConfigured()
        {
            _registry.CanRead = false;

            Assert.Null(await _context.GetLatestAsync());
        }
    }
}
