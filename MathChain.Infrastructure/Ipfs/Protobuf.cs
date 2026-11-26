namespace MathChain.Infrastructure.Ipfs
{
    public static class Protobuf
    {
        public static void WriteVarint(List<byte> output, int field, ulong value)
        {
            Varint.Write(output, (ulong)(field << 3));
            Varint.Write(output, value);
        }

        public static void WriteBytes(List<byte> output, int field, ReadOnlySpan<byte> value)
        {
            Varint.Write(output, (ulong)((field << 3) | 2));
            Varint.Write(output, (ulong)value.Length);
            output.AddRange(value.ToArray());
        }

        public ref struct Reader
        {
            private readonly ReadOnlySpan<byte> _bytes;
            private int _offset;

            public Reader(ReadOnlySpan<byte> bytes)
            {
                _bytes = bytes;
                _offset = 0;
            }

            public bool TryReadField(out int field, out int wireType)
            {
                field = 0;
                wireType = 0;

                if (_offset >= _bytes.Length)
                {
                    return false;
                }

                var key = ReadVarint();
                field = (int)(key >> 3);
                wireType = (int)(key & 0x7);

                if (field == 0)
                {
                    throw new FormatException("A protobuf field number cannot be zero.");
                }

                return true;
            }

            public bool TryReadPacked(out ulong value)
            {
                value = 0;

                if (_offset >= _bytes.Length)
                {
                    return false;
                }

                value = ReadVarint();
                return true;
            }

            public ulong ReadVarint()
            {
                if (!Varint.TryRead(_bytes[_offset..], out var value, out var length))
                {
                    throw new FormatException("A protobuf varint is not valid.");
                }

                _offset += length;
                return value;
            }

            public ReadOnlySpan<byte> ReadBytes()
            {
                var length = ReadVarint();

                if (length > (ulong)(_bytes.Length - _offset))
                {
                    throw new FormatException("A protobuf field is longer than the block.");
                }

                var value = _bytes.Slice(_offset, (int)length);
                _offset += (int)length;

                return value;
            }
        }
    }
}
