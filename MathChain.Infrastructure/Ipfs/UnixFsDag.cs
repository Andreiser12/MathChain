namespace MathChain.Infrastructure.Ipfs
{
    public sealed record ContentBlockData(ContentId Cid, byte[] Bytes);

    public sealed record UnixFsFile(ContentId Root, IReadOnlyList<ContentBlockData> Blocks);

    public static class UnixFsDag
    {
        public const int DefaultChunkSize = 262144;
        public const int DefaultMaximumChildren = 174;

        private sealed record Node(ContentId Cid, ulong FileSize, ulong TotalSize);

        public static UnixFsFile Build(ReadOnlySpan<byte> content, int chunkSize = DefaultChunkSize, int maximumChildren = DefaultMaximumChildren)
        {
            return BuildTree(content, chunkSize, maximumChildren, chunk =>
            {
                var cid = ContentId.ForBlock(ContentCodec.Raw, chunk);
                return (cid, chunk);
            }, block => ContentId.ForBlock(ContentCodec.DagPb, block));
        }

        public static UnixFsFile BuildVersionZero(ReadOnlySpan<byte> content, ulong leafType, int chunkSize = DefaultChunkSize,
            int maximumChildren = DefaultMaximumChildren)
        {
            return BuildTree(content, chunkSize, maximumChildren, chunk =>
            {
                var data = new UnixFsData(leafType, chunk.Length == 0 ? null : chunk, (ulong)chunk.Length, Array.Empty<ulong>()).Encode();
                var block = new DagPbNode(Array.Empty<DagPbLink>(), data).Encode();
                return (ContentId.ForVersionZeroBlock(block), block);
            }, block => ContentId.ForVersionZeroBlock(block));
        }

        private static UnixFsFile BuildTree(ReadOnlySpan<byte> content, int chunkSize, int maximumChildren,
            Func<byte[], (ContentId Cid, byte[] Block)> buildLeaf, Func<byte[], ContentId> identifyParent)
        {
            if (chunkSize <= 0 || maximumChildren < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(chunkSize), "The chunk size must be positive and a node must allow at least two children.");
            }

            var blocks = new List<ContentBlockData>();
            var level = new List<Node>();

            for (var offset = 0; offset < content.Length || level.Count == 0; offset += chunkSize)
            {
                var chunk = content.Slice(offset, Math.Min(chunkSize, content.Length - offset)).ToArray();
                var (cid, block) = buildLeaf(chunk);
                blocks.Add(new ContentBlockData(cid, block));
                level.Add(new Node(cid, (ulong)chunk.Length, (ulong)block.Length));
            }

            while (level.Count > 1)
            {
                level = level.Chunk(maximumChildren).Select(group => BuildParent(group, blocks, identifyParent)).ToList();
            }

            return new UnixFsFile(level[0].Cid, blocks);
        }

        private static Node BuildParent(IReadOnlyList<Node> children, List<ContentBlockData> blocks, Func<byte[], ContentId> identify)
        {
            var fileSize = children.Aggregate(0UL, (sum, child) => sum + child.FileSize);
            var data = new UnixFsData(UnixFsData.FileType, null, fileSize, children.Select(c => c.FileSize).ToList()).Encode();
            var block = new DagPbNode(children.Select(c => new DagPbLink(c.Cid, c.TotalSize)).ToList(), data).Encode();
            var cid = identify(block);
            blocks.Add(new ContentBlockData(cid, block));

            return new Node(cid, fileSize, (ulong)block.Length + children.Aggregate(0UL, (sum, child) => sum + child.TotalSize));
        }

        public static async Task<byte[]> ReadAsync(ContentId root, Func<IReadOnlyCollection<ContentId>, Task<IReadOnlyDictionary<ContentId, byte[]>>> fetch,
            long maximumSize)
        {
            var output = new MemoryStream();
            var blocks = await fetch(new[] { root });
            await AppendAsync(root, Load(root, blocks), fetch, output, maximumSize, 0);

            return output.ToArray();
        }

        private static async Task<ulong> AppendAsync(ContentId cid, byte[] block,
            Func<IReadOnlyCollection<ContentId>, Task<IReadOnlyDictionary<ContentId, byte[]>>> fetch, MemoryStream output, long maximumSize, int depth)
        {
            if (depth > 64)
            {
                throw new FormatException("The content graph is too deep.");
            }

            if (cid.Codec == ContentCodec.Raw)
            {
                Write(output, block, maximumSize);
                return (ulong)block.Length;
            }

            var node = DagPbNode.Decode(block);
            var unixFs = UnixFsData.Decode(node.Data ?? throw new FormatException("The dag-pb node has no UnixFS data."));

            if (unixFs.Type != UnixFsData.FileType && unixFs.Type != UnixFsData.RawType)
            {
                throw new FormatException("Only UnixFS files are supported.");
            }

            ulong written = 0;

            if (unixFs.Data != null)
            {
                Write(output, unixFs.Data, maximumSize);
                written += (ulong)unixFs.Data.Length;
            }

            if (node.Links.Count > 0)
            {
                var children = await fetch(node.Links.Select(l => l.Cid).Distinct().ToList());

                foreach (var link in node.Links)
                {
                    written += await AppendAsync(link.Cid, Load(link.Cid, children), fetch, output, maximumSize, depth + 1);
                }
            }

            if (unixFs.FileSize.HasValue && unixFs.FileSize.Value != written)
            {
                throw new FormatException("The UnixFS file size does not match its content.");
            }

            return written;
        }

        private static byte[] Load(ContentId cid, IReadOnlyDictionary<ContentId, byte[]> blocks)
        {
            if (!blocks.TryGetValue(cid, out var block))
            {
                throw new KeyNotFoundException($"Block {cid} is missing.");
            }

            if (!cid.Matches(block))
            {
                throw new InvalidDataException($"Block {cid} does not match its content identifier.");
            }

            return block;
        }

        private static void Write(MemoryStream output, byte[] bytes, long maximumSize)
        {
            if (output.Length + bytes.Length > maximumSize)
            {
                throw new InvalidDataException("The content is larger than allowed.");
            }

            output.Write(bytes);
        }
    }
}
