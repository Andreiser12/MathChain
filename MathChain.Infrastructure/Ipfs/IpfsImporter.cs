using MathChain.Domain.Entities;
using MathChain.Domain.Gateways;

namespace MathChain.Infrastructure.Ipfs
{
    public sealed class IpfsImportReport
    {
        public int Imported { get; set; }
        public int AlreadyStored { get; set; }
        public List<string> Rejected { get; } = new();
        public List<ContentReference> MissingReferences { get; } = new();
    }

    public class IpfsImporter
    {
        private readonly IContentBlockGateway _blocks;
        private readonly TimeProvider _timeProvider;

        public IpfsImporter(IContentBlockGateway blocks, TimeProvider? timeProvider = null)
        {
            _blocks = blocks;
            _timeProvider = timeProvider ?? TimeProvider.System;
        }

        public async Task<IpfsImportReport> ImportFolderAsync(string folder)
        {
            if (!Directory.Exists(folder))
            {
                throw new DirectoryNotFoundException($"The folder {folder} does not exist.");
            }

            var report = new IpfsImportReport();

            foreach (var path in Directory.EnumerateFiles(folder).OrderBy(p => p, StringComparer.Ordinal))
            {
                var name = Path.GetFileName(path);
                var cidText = Path.GetFileNameWithoutExtension(name);

                if (!ContentId.TryParse(cidText, out var cid) || !cid!.IsVersionZero)
                {
                    report.Rejected.Add($"{name}: the name is not a content identifier");
                    continue;
                }

                var content = await File.ReadAllBytesAsync(path);
                var now = _timeProvider.GetUtcNow().UtcDateTime;
                var blocks = BlocksFor(cid, content, now);

                if (blocks == null)
                {
                    report.Rejected.Add($"{name}: the content does not match the name");
                    continue;
                }

                var added = await _blocks.AddMissingAsync(blocks);

                if (added > 0)
                {
                    report.Imported++;
                }
                else
                {
                    report.AlreadyStored++;
                }
            }

            foreach (var reference in await _blocks.GetReferencesAsync())
            {
                if (!ContentId.TryParse(reference.Cid, out _) || !await _blocks.ExistsAsync(reference.Cid))
                {
                    report.MissingReferences.Add(reference);
                }
            }

            return report;
        }

        private static List<ContentBlock>? BlocksFor(ContentId cid, byte[] content, DateTime now)
        {
            if (cid.Matches(content))
            {
                return new List<ContentBlock>
                {
                    new() { Cid = cid.ToString(), Codec = IpfsStorageService.LegacyFileCodec, Data = content, Size = content.Length, CreatedAt = now }
                };
            }

            foreach (var leafType in new[] { UnixFsData.FileType, UnixFsData.RawType })
            {
                var file = UnixFsDag.BuildVersionZero(content, leafType);

                if (file.Root.Equals(cid))
                {
                    return file.Blocks.Select(block => new ContentBlock
                    {
                        Cid = block.Cid.ToString(),
                        Codec = (int)ContentCodec.DagPb,
                        Data = block.Bytes,
                        Size = block.Bytes.Length,
                        CreatedAt = now
                    }).ToList();
                }
            }

            return null;
        }
    }
}
