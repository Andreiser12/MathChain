using System.Security.Cryptography;

namespace MathChain.Infrastructure.Ipfs
{
    public enum ContentCodec : ulong
    {
        Raw = 0x55,
        DagPb = 0x70
    }

    public sealed class ContentId : IEquatable<ContentId>
    {
        public const int DigestLength = 32;
        private const ulong Sha256Code = 0x12;
        private const int LegacyTextLength = 46;
        private const int VersionOneTextLength = 59;

        private readonly byte[] _digest;

        private ContentId(int version, ContentCodec codec, byte[] digest)
        {
            Version = version;
            Codec = codec;
            _digest = digest;
        }

        public int Version { get; }
        public ContentCodec Codec { get; }
        public bool IsVersionZero => Version == 0;
        public ReadOnlySpan<byte> Digest => _digest;

        public static ContentId ForBlock(ContentCodec codec, ReadOnlySpan<byte> block)
        {
            return new ContentId(1, codec, SHA256.HashData(block));
        }

        public static ContentId ForVersionZeroBlock(ReadOnlySpan<byte> block)
        {
            return new ContentId(0, ContentCodec.DagPb, SHA256.HashData(block));
        }

        public bool Matches(ReadOnlySpan<byte> bytes)
        {
            return CryptographicOperations.FixedTimeEquals(SHA256.HashData(bytes), _digest);
        }

        public byte[] ToBytes()
        {
            var output = new List<byte>(36);

            if (!IsVersionZero)
            {
                Varint.Write(output, 1);
                Varint.Write(output, (ulong)Codec);
            }

            Varint.Write(output, Sha256Code);
            Varint.Write(output, DigestLength);
            output.AddRange(_digest);

            return output.ToArray();
        }

        public override string ToString()
        {
            return IsVersionZero ? Base58.Encode(ToBytes()) : "b" + Base32.Encode(ToBytes());
        }

        public static ContentId Parse(string text)
        {
            return TryParse(text, out var cid) ? cid! : throw new FormatException("The value is not a valid content identifier.");
        }

        public static bool TryParse(string? text, out ContentId? cid)
        {
            cid = null;

            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            if (text.Length == LegacyTextLength && text.StartsWith("Qm", StringComparison.Ordinal))
            {
                if (!Base58.TryDecode(text, out var legacyBytes) || legacyBytes.Length != 34
                    || legacyBytes[0] != Sha256Code || legacyBytes[1] != DigestLength)
                {
                    return false;
                }

                cid = new ContentId(0, ContentCodec.DagPb, legacyBytes[2..]);
            }
            else if (text.Length == VersionOneTextLength && text[0] == 'b')
            {
                if (!Base32.TryDecode(text[1..], out var bytes) || !TryParseBinary(bytes, out cid))
                {
                    return false;
                }
            }
            else
            {
                return false;
            }

            if (cid!.ToString() != text)
            {
                cid = null;
                return false;
            }

            return true;
        }

        public static bool TryParseBinary(ReadOnlySpan<byte> bytes, out ContentId? cid)
        {
            cid = null;
            var offset = 0;

            if (bytes.Length == DigestLength + 2 && bytes[0] == Sha256Code && bytes[1] == DigestLength)
            {
                cid = new ContentId(0, ContentCodec.DagPb, bytes[2..].ToArray());
                return true;
            }

            if (!Varint.TryRead(bytes, out var version, out var read) || version != 1)
            {
                return false;
            }

            offset += read;

            if (!Varint.TryRead(bytes[offset..], out var codec, out read) || (codec != (ulong)ContentCodec.Raw && codec != (ulong)ContentCodec.DagPb))
            {
                return false;
            }

            offset += read;

            if (!Varint.TryRead(bytes[offset..], out var hashCode, out read) || hashCode != Sha256Code)
            {
                return false;
            }

            offset += read;

            if (!Varint.TryRead(bytes[offset..], out var length, out read) || length != DigestLength)
            {
                return false;
            }

            offset += read;

            if (bytes.Length - offset != DigestLength)
            {
                return false;
            }

            cid = new ContentId(1, (ContentCodec)codec, bytes[offset..].ToArray());
            return true;
        }

        public bool Equals(ContentId? other)
        {
            return other != null && other.Version == Version && other.Codec == Codec && other._digest.AsSpan().SequenceEqual(_digest);
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as ContentId);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Version, Codec, BitConverter.ToInt32(_digest, 0));
        }
    }
}
