using System.Security.Cryptography;
using System.Text;
using MathChain.Domain.Entities;
using MathChain.Domain.Exceptions;
using MathChain.Domain.Interfaces;
using MathChain.Infrastructure.Ipfs;
using MathChain.Tests.Shared.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace MathChain.Infrastructure.UnitTests
{
    public class IpfsStorageServiceTests
    {
        private readonly InMemoryContentBlockGateway _blocks = new();
        private readonly IpfsStorageService _service;

        public IpfsStorageServiceTests()
        {
            _service = new IpfsStorageService(_blocks, NullLogger<IpfsStorageService>.Instance);
        }

        private static byte[] Content(int size)
        {
            var bytes = new byte[size];
            for (var i = 0; i < size; i++)
            {
                bytes[i] = (byte)((i * 31 + 7) % 251);
            }

            return bytes;
        }

        private static string LegacyCid(byte[] content)
        {
            return Base58.Encode(new byte[] { 0x12, 0x20 }.Concat(SHA256.HashData(content)).ToArray());
        }

        [Fact]
        public async Task SaveFileAsync_ShouldStoreOneRawBlockWithTheStandardCid_WhenTheFileIsSmall()
        {
            var cid = await _service.SaveFileAsync(Encoding.UTF8.GetBytes("MathChain curriculum\n"));

            Assert.Equal("bafkreih77q4xcux2oixp2lq5mm5ithbcxniucznvyeeqxp3wxiacksuyya", cid);
            Assert.Single(_blocks.Blocks);
            Assert.Equal((int)ContentCodec.Raw, _blocks.Blocks[cid].Codec);
        }

        [Fact]
        public async Task SaveFileAsync_ShouldStoreAMerkleDag_WhenTheFileIsLarge()
        {
            var content = Content(2 * 262144 + 10);

            var cid = await _service.SaveFileAsync(content);

            Assert.Equal("bafybeie47ugxb6ive4whedv5sx7kgvsb4kpxqxvnxx547ck635s4sq3guq", cid);
            Assert.Equal(4, _blocks.Blocks.Count);
            Assert.Equal(content, await _service.GetFileAsync(cid));
        }

        [Fact]
        public async Task SaveFileAsync_ShouldAcceptTheFile_WhenItHasTenMegabytes()
        {
            var content = Content(IIpfsStorageService.MaximumFileSize);

            var cid = await _service.SaveFileAsync(content);

            Assert.Equal("bafybeie64rm3zbbks4cbfdqetj73fycbh3cog2oja3mqv4lhhs7w23clyq", cid);
            Assert.Equal(content, await _service.GetFileAsync(cid));
        }

        [Fact]
        public async Task SaveFileAsync_ShouldStoreTheBlocksOnce_WhenTheSameFileIsSavedTwice()
        {
            var content = Content(300000);

            var first = await _service.SaveFileAsync(content);
            var count = _blocks.Blocks.Count;
            var second = await _service.SaveFileAsync(content);

            Assert.Equal(first, second);
            Assert.Equal(count, _blocks.Blocks.Count);
        }

        [Fact]
        public async Task SaveFileAsync_ShouldThrowBusinessRuleException_WhenTheFileIsEmptyOrTooLarge()
        {
            await Assert.ThrowsAsync<BusinessRuleException>(() => _service.SaveFileAsync(Array.Empty<byte>()));
            var exception = await Assert.ThrowsAsync<BusinessRuleException>(() => _service.SaveFileAsync(new byte[IIpfsStorageService.MaximumFileSize + 1]));

            Assert.Contains("10 MB", exception.Message);
            Assert.Empty(_blocks.Blocks);
        }

        [Fact]
        public async Task GetFileAsync_ShouldReturnNothing_WhenABlockIsTampered()
        {
            var content = Content(300000);
            var cid = await _service.SaveFileAsync(content);
            var leaf = _blocks.Blocks.Values.First(b => b.Codec == (int)ContentCodec.Raw);
            leaf.Data[10] ^= 0xFF;

            Assert.Null(await _service.GetFileAsync(cid));
        }

        [Fact]
        public async Task GetFileAsync_ShouldReturnNothing_WhenABlockIsMissing()
        {
            var cid = await _service.SaveFileAsync(Content(300000));
            _blocks.Blocks.Remove(_blocks.Blocks.Keys.First(k => k != cid));

            Assert.Null(await _service.GetFileAsync(cid));
        }

        [Fact]
        public async Task GetFileAsync_ShouldVerifyTheContentWithTheOldAlgorithm_WhenTheContentIsLegacy()
        {
            var content = Encoding.UTF8.GetBytes("old chapter snapshot");
            var cid = LegacyCid(content);
            _blocks.Blocks[cid] = new ContentBlock { Cid = cid, Codec = 0, Data = content, Size = content.Length };

            Assert.Equal(content, await _service.GetFileAsync(cid));
            Assert.True(await _service.FileExistsAsync(cid));

            _blocks.Blocks[cid].Data[0] ^= 0x01;
            Assert.Null(await _service.GetFileAsync(cid));
        }

        [Theory]
        [InlineData("..\\..\\appsettings.json")]
        [InlineData("Qm")]
        [InlineData("not-a-cid")]
        [InlineData("")]
        public async Task GetFileAsync_ShouldReturnNothing_WhenTheIdentifierIsInvalid(string value)
        {
            Assert.Null(await _service.GetFileAsync(value));
            Assert.False(await _service.FileExistsAsync(value));
        }

        [Fact]
        public void ComputeHash_ShouldNotStoreAnything_WhenTheHashIsComputed()
        {
            Assert.Equal("bafkreihdwdcefgh4dqkjv67uzcmw7ojee6xedzdetojuzjevtenxquvyku", _service.ComputeHash(Array.Empty<byte>()));
            Assert.Empty(_blocks.Blocks);
        }
    }
}
