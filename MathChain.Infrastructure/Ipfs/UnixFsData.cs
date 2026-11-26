namespace MathChain.Infrastructure.Ipfs
{
    public sealed class UnixFsData
    {
        public const ulong FileType = 2;
        public const ulong RawType = 0;
        private const int TypeField = 1;
        private const int DataField = 2;
        private const int FileSizeField = 3;
        private const int BlockSizesField = 4;

        public UnixFsData(ulong type, byte[]? data, ulong? fileSize, IReadOnlyList<ulong> blockSizes)
        {
            Type = type;
            Data = data;
            FileSize = fileSize;
            BlockSizes = blockSizes;
        }

        public ulong Type { get; }
        public byte[]? Data { get; }
        public ulong? FileSize { get; }
        public IReadOnlyList<ulong> BlockSizes { get; }

        public byte[] Encode()
        {
            var output = new List<byte>();
            Protobuf.WriteVarint(output, TypeField, Type);

            if (Data != null)
            {
                Protobuf.WriteBytes(output, DataField, Data);
            }

            if (FileSize.HasValue)
            {
                Protobuf.WriteVarint(output, FileSizeField, FileSize.Value);
            }

            foreach (var size in BlockSizes)
            {
                Protobuf.WriteVarint(output, BlockSizesField, size);
            }

            return output.ToArray();
        }

        public static UnixFsData Decode(ReadOnlySpan<byte> bytes)
        {
            ulong? type = null;
            byte[]? data = null;
            ulong? fileSize = null;
            var blockSizes = new List<ulong>();
            var reader = new Protobuf.Reader(bytes);

            while (reader.TryReadField(out var field, out var wireType))
            {
                switch (field, wireType)
                {
                    case (TypeField, 0):
                        type = reader.ReadVarint();
                        break;
                    case (DataField, 2):
                        data = reader.ReadBytes().ToArray();
                        break;
                    case (FileSizeField, 0):
                        fileSize = reader.ReadVarint();
                        break;
                    case (BlockSizesField, 0):
                        blockSizes.Add(reader.ReadVarint());
                        break;
                    case (BlockSizesField, 2):
                        var packed = new Protobuf.Reader(reader.ReadBytes());
                        while (packed.TryReadPacked(out var size))
                        {
                            blockSizes.Add(size);
                        }
                        break;
                    default:
                        throw new FormatException("The UnixFS data is not a supported file node.");
                }
            }

            return new UnixFsData(type ?? throw new FormatException("The UnixFS data has no type."), data, fileSize, blockSizes);
        }
    }
}
