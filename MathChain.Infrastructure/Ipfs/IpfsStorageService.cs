using MathChain.Domain.Entities;
using MathChain.Domain.Exceptions;
using MathChain.Domain.Gateways;
using MathChain.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MathChain.Infrastructure.Ipfs
{
    public class IpfsStorageService : IIpfsStorageService
    {
        public const int LegacyFileCodec = 0;

        private readonly IContentBlockGateway _blocks;
        private readonly ILogger<IpfsStorageService> _logger;
        private readonly TimeProvider _timeProvider;

        public IpfsStorageService(IContentBlockGateway blocks, ILogger<IpfsStorageService> logger, TimeProvider? timeProvider = null)
        {
            _blocks = blocks;
            _logger = logger;
            _timeProvider = timeProvider ?? TimeProvider.System;
        }

        public string ComputeHash(byte[] content)
        {
            return UnixFsDag.Build(content).Root.ToString();
        }

        public async Task<string> SaveFileAsync(byte[] content)
        {
            if (content == null || content.Length == 0)
            {
                throw new BusinessRuleException("File content is empty.");
            }

            if (content.Length > IIpfsStorageService.MaximumFileSize)
            {
                throw new BusinessRuleException("Files can be at most 10 MB.");
            }

            var file = UnixFsDag.Build(content);
            var now = _timeProvider.GetUtcNow().UtcDateTime;

            await _blocks.AddMissingAsync(file.Blocks.Select(block => new ContentBlock
            {
                Cid = block.Cid.ToString(),
                Codec = (int)block.Cid.Codec,
                Data = block.Bytes,
                Size = block.Bytes.Length,
                CreatedAt = now
            }).ToList());

            return file.Root.ToString();
        }

        public async Task<byte[]?> GetFileAsync(string ipfsHash)
        {
            if (!ContentId.TryParse(ipfsHash, out var root))
            {
                return null;
            }

            try
            {
                if (root!.IsVersionZero)
                {
                    var stored = (await _blocks.GetManyAsync(new[] { ipfsHash })).SingleOrDefault();

                    if (stored == null)
                    {
                        return null;
                    }

                    if (stored.Codec == LegacyFileCodec)
                    {
                        return root.Matches(stored.Data)
                            ? stored.Data
                            : throw new InvalidDataException($"Block {ipfsHash} does not match its content identifier.");
                    }
                }

                return await UnixFsDag.ReadAsync(root, FetchAsync, IIpfsStorageService.MaximumFileSize);
            }
            catch (KeyNotFoundException)
            {
                return null;
            }
            catch (Exception exception) when (exception is InvalidDataException or FormatException)
            {
                _logger.LogError(exception, "Stored content {Cid} failed verification", ipfsHash);
                return null;
            }
        }

        public async Task<bool> FileExistsAsync(string ipfsHash)
        {
            return ContentId.TryParse(ipfsHash, out _) && await _blocks.ExistsAsync(ipfsHash);
        }

        public byte[] ToBinaryCid(string cid)
        {
            return ContentId.Parse(cid).ToBytes();
        }

        public string? FromBinaryCid(byte[] cid)
        {
            return ContentId.TryParseBinary(cid, out var parsed) ? parsed!.ToString() : null;
        }

                private async Task<IReadOnlyDictionary<ContentId, byte[]>> FetchAsync(IReadOnlyCollection<ContentId> cids)
        {
            var stored = await _blocks.GetManyAsync(cids.Select(c => c.ToString()).ToList());

            return stored.ToDictionary(block => ContentId.Parse(block.Cid), block => block.Data);
        }
    }
}
