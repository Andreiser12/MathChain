namespace MathChain.Infrastructure.Ipfs
{
    public sealed record DagPbLink(ContentId Cid, ulong TotalSize);

    public sealed class DagPbNode
    {
        private const int LinksField = 2;
        private const int DataField = 1;
        private const int LinkHashField = 1;
        private const int LinkNameField = 2;
        private const int LinkSizeField = 3;
        private const int LengthDelimited = 2;
        private const int VarintType = 0;

        public DagPbNode(IReadOnlyList<DagPbLink> links, byte[]? data)
        {
            Links = links;
            Data = data;
        }

        public IReadOnlyList<DagPbLink> Links { get; }
        public byte[]? Data { get; }

        public byte[] Encode()
        {
            var output = new List<byte>();

            foreach (var link in Links)
            {
                var encodedLink = new List<byte>();
                Protobuf.WriteBytes(encodedLink, LinkHashField, link.Cid.ToBytes());
                Protobuf.WriteBytes(encodedLink, LinkNameField, Array.Empty<byte>());
                Protobuf.WriteVarint(encodedLink, LinkSizeField, link.TotalSize);
                Protobuf.WriteBytes(output, LinksField, encodedLink.ToArray());
            }

            if (Data != null)
            {
                Protobuf.WriteBytes(output, DataField, Data);
            }

            return output.ToArray();
        }

        public static DagPbNode Decode(ReadOnlySpan<byte> block)
        {
            var links = new List<DagPbLink>();
            byte[]? data = null;
            var reader = new Protobuf.Reader(block);

            while (reader.TryReadField(out var field, out var wireType))
            {
                if (field == LinksField && wireType == LengthDelimited && data == null)
                {
                    links.Add(DecodeLink(reader.ReadBytes()));
                }
                else if (field == DataField && wireType == LengthDelimited && data == null)
                {
                    data = reader.ReadBytes().ToArray();
                }
                else
                {
                    throw new FormatException("The block is not a valid dag-pb node.");
                }
            }

            return new DagPbNode(links, data);
        }

        private static DagPbLink DecodeLink(ReadOnlySpan<byte> bytes)
        {
            ContentId? cid = null;
            ulong? size = null;
            var reader = new Protobuf.Reader(bytes);

            while (reader.TryReadField(out var field, out var wireType))
            {
                if (field == LinkHashField && wireType == LengthDelimited && cid == null)
                {
                    if (!ContentId.TryParseBinary(reader.ReadBytes(), out cid))
                    {
                        throw new FormatException("A dag-pb link has an unsupported content identifier.");
                    }
                }
                else if (field == LinkNameField && wireType == LengthDelimited)
                {
                    reader.ReadBytes();
                }
                else if (field == LinkSizeField && wireType == VarintType && size == null)
                {
                    size = reader.ReadVarint();
                }
                else
                {
                    throw new FormatException("A dag-pb link is not valid.");
                }
            }

            return new DagPbLink(cid ?? throw new FormatException("A dag-pb link has no hash."), size ?? 0);
        }
    }
}
