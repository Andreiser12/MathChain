using System.Security.Cryptography;
using System.Text;
using MathChain.Domain.Entities;
using MathChain.Infrastructure.Ipfs;
using MathChain.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace MathChain.Tests.Infrastructure
{
    public sealed class IpfsImporterTests : IDisposable
    {
        private readonly string _folder = Path.Combine(Path.GetTempPath(), "ipfs-import-" + Guid.NewGuid().ToString("N"));
        private readonly InMemoryContentBlockGateway _blocks = new();

        public IpfsImporterTests()
        {
            Directory.CreateDirectory(_folder);
        }

        public void Dispose()
        {
            Directory.Delete(_folder, true);
        }

        private string WriteLegacy(string text, string extension = "")
        {
            var content = Encoding.UTF8.GetBytes(text);
            var cid = Base58.Encode(new byte[] { 0x12, 0x20 }.Concat(SHA256.HashData(content)).ToArray());
            File.WriteAllBytes(Path.Combine(_folder, cid + extension), content);
            return cid;
        }

        [Fact]
        public async Task ImportFolderAsync_ShouldImportTheVerifiedFilesAndReportTheRest_WhenTheFolderHasMixedFiles()
        {
            var plain = WriteLegacy("chapter snapshot");
            var withExtension = WriteLegacy("image bytes", ".png");
            var tampered = WriteLegacy("original");
            File.WriteAllText(Path.Combine(_folder, tampered), "changed");
            File.WriteAllText(Path.Combine(_folder, "notes.txt"), "not content addressed");
            _blocks.References.Add(new ContentReference("CurriculumChapters", Guid.NewGuid(), plain));
            _blocks.References.Add(new ContentReference("Submissions", Guid.NewGuid(), "QmYTNyRmU9Ms8voG3bSKm9xVvVH4om65q7FuiYuDLGDpMs"));
            _blocks.References.Add(new ContentReference("ClassMedia", Guid.NewGuid(), "broken value"));

            var report = await new IpfsImporter(_blocks).ImportFolderAsync(_folder);
            var service = new IpfsStorageService(_blocks, NullLogger<IpfsStorageService>.Instance);

            Assert.Equal(2, report.Imported);
            Assert.Equal(2, report.Rejected.Count);
            Assert.Contains(report.Rejected, r => r.StartsWith(tampered) && r.Contains("does not match"));
            Assert.Contains(report.Rejected, r => r.StartsWith("notes.txt"));
            Assert.Equal(new[] { "Submissions", "ClassMedia" }, report.MissingReferences.Select(r => r.Source));
            Assert.Equal("chapter snapshot", Encoding.UTF8.GetString((await service.GetFileAsync(plain))!));
            Assert.Equal("image bytes", Encoding.UTF8.GetString((await service.GetFileAsync(withExtension))!));
            Assert.Null(await service.GetFileAsync(tampered));
        }

        [Theory]
        [InlineData("QmXoNkP3DBcSVpFSPw5WZrxs2LvbKDGtvQSmhxNXYYVtwJ")]
        [InlineData("QmZKHh6Tx6USLZX5aNBmLpVA7hYA1qo9bHbMVfsgAWfz66")]
        public async Task ImportFolderAsync_ShouldImportTheFileAsItsDag_WhenItIsAGenuineIpfsVersionZeroFile(string referenceCid)
        {
            var content = new byte[600000];
            for (var i = 0; i < content.Length; i++)
            {
                content[i] = (byte)((i * 31 + 7) % 251);
            }
            File.WriteAllBytes(Path.Combine(_folder, referenceCid), content);

            var report = await new IpfsImporter(_blocks).ImportFolderAsync(_folder);
            var service = new IpfsStorageService(_blocks, NullLogger<IpfsStorageService>.Instance);

            Assert.Equal(1, report.Imported);
            Assert.Empty(report.Rejected);
            Assert.Equal(4, _blocks.Blocks.Count);
            Assert.Equal(content, await service.GetFileAsync(referenceCid));
        }

        [Fact]
        public async Task ImportFolderAsync_ShouldNotDuplicateAnything_WhenItRunsTwice()
        {
            WriteLegacy("chapter snapshot");
            var importer = new IpfsImporter(_blocks);

            await importer.ImportFolderAsync(_folder);
            var second = await importer.ImportFolderAsync(_folder);

            Assert.Equal(0, second.Imported);
            Assert.Equal(1, second.AlreadyStored);
            Assert.Single(_blocks.Blocks);
        }

        [Fact]
        public async Task ImportFolderAsync_ShouldThrow_WhenTheFolderIsMissing()
        {
            await Assert.ThrowsAsync<DirectoryNotFoundException>(() => new IpfsImporter(_blocks).ImportFolderAsync(_folder + "-missing"));
        }
    }
}
