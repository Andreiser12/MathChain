using System.Security.Cryptography;
using System.Text;
using MathChain.Infrastructure.Ipfs;

namespace MathChain.Tests.Infrastructure
{
    public class ContentIdTests
    {
        private const string EmptyRaw = "bafkreihdwdcefgh4dqkjv67uzcmw7ojee6xedzdetojuzjevtenxquvyku";

        [Theory]
        [InlineData(0UL, "00")]
        [InlineData(1UL, "01")]
        [InlineData(127UL, "7f")]
        [InlineData(128UL, "8001")]
        [InlineData(300UL, "ac02")]
        [InlineData(16384UL, "808001")]
        public void Varint_EncodesAndDecodes(ulong value, string hex)
        {
            Assert.Equal(hex, Convert.ToHexString(Varint.Encode(value)).ToLowerInvariant());
            Assert.True(Varint.TryRead(Convert.FromHexString(hex), out var decoded, out var length));
            Assert.Equal(value, decoded);
            Assert.Equal(hex.Length / 2, length);
        }

        [Theory]
        [InlineData("80")]
        [InlineData("8000")]
        [InlineData("")]
        public void Varint_TruncatedOrOverlong_IsRejected(string hex)
        {
            Assert.False(Varint.TryRead(Convert.FromHexString(hex), out _, out _));
        }

        [Theory]
        [InlineData("", "")]
        [InlineData("f", "my")]
        [InlineData("fo", "mzxq")]
        [InlineData("foo", "mzxw6")]
        [InlineData("foob", "mzxw6yq")]
        [InlineData("fooba", "mzxw6ytb")]
        [InlineData("foobar", "mzxw6ytboi")]
        public void Base32_MatchesRfc4648(string text, string encoded)
        {
            Assert.Equal(encoded, Base32.Encode(Encoding.ASCII.GetBytes(text)));
            Assert.True(Base32.TryDecode(encoded, out var decoded));
            Assert.Equal(text, Encoding.ASCII.GetString(decoded));
        }

        [Theory]
        [InlineData("MZXW6")]
        [InlineData("mzxw7")]
        [InlineData("mzxw6=")]
        [InlineData("m")]
        public void Base32_InvalidOrNonCanonical_IsRejected(string encoded)
        {
            Assert.False(Base32.TryDecode(encoded, out _));
        }

        [Fact]
        public void ForBlock_EmptyRawBlock_IsTheWellKnownEmptyCid()
        {
            Assert.Equal(EmptyRaw, ContentId.ForBlock(ContentCodec.Raw, Array.Empty<byte>()).ToString());
        }

        [Fact]
        public void Parse_VersionOne_RoundTrips()
        {
            var cid = ContentId.Parse(EmptyRaw);

            Assert.Equal(1, cid.Version);
            Assert.Equal(ContentCodec.Raw, cid.Codec);
            Assert.True(cid.Matches(Array.Empty<byte>()));
            Assert.Equal(EmptyRaw, cid.ToString());
            Assert.Equal(cid, ContentId.Parse(EmptyRaw));
        }

        [Fact]
        public void Parse_LegacyIdentifier_UsesTheHashOfTheWholeFile()
        {
            var content = Encoding.UTF8.GetBytes("legacy file");
            var multihash = new byte[] { 0x12, 0x20 }.Concat(SHA256.HashData(content)).ToArray();
            var text = Base58.Encode(multihash);

            var cid = ContentId.Parse(text);

            Assert.True(cid.IsVersionZero);
            Assert.Equal(ContentCodec.DagPb, cid.Codec);
            Assert.StartsWith("Qm", text);
            Assert.Equal(text, cid.ToString());
            Assert.True(cid.Matches(content));
            Assert.Equal(text, ContentId.ForVersionZeroBlock(content).ToString());
        }

        [Fact]
        public void Parse_ExistingLegacyIdentifierFromTheRepository_IsAccepted()
        {
            Assert.True(ContentId.TryParse("QmYTNyRmU9Ms8voG3bSKm9xVvVH4om65q7FuiYuDLGDpMs", out var cid));
            Assert.True(cid!.IsVersionZero);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("..\\..\\appsettings.json")]
        [InlineData("../appsettings.json")]
        [InlineData("Qm")]
        [InlineData("QmYTNyRmU9Ms8voG3bSKm9xVvVH4om65q7FuiYuDLGDpM0")]
        [InlineData("QmYTNyRmU9Ms8voG3bSKm9xVvVH4om65q7FuiYuDLGDpMs.png")]
        [InlineData("BAFKREIHDWDCEFGH4DQKJV67UZCMW7OJEE6XEDZDETOJUZJEVTENXQUVYKU")]
        [InlineData("bafkreihdwdcefgh4dqkjv67uzcmw7ojee6xedzdetojuzjevtenxquvykv")]
        [InlineData("bafkreihdwdcefgh4dqkjv67uzcmw7ojee6xedzdetojuzjevtenxquvyk")]
        [InlineData("zdj7WWeQ43G6JJvLWQWZpyHuAMq6uYWRjkBXFad11vE2LHhQ7")]
        public void TryParse_InvalidValues_AreRejected(string? text)
        {
            Assert.False(ContentId.TryParse(text, out var cid));
            Assert.Null(cid);
        }

        [Fact]
        public void TryParse_UnsupportedCodec_IsRejected()
        {
            var bytes = new List<byte> { 0x01, 0x71, 0x12, 0x20 };
            bytes.AddRange(new byte[32]);

            Assert.False(ContentId.TryParse("b" + Base32.Encode(bytes.ToArray()), out _));
        }

        [Fact]
        public void TryParseBinary_VersionZeroMultihash_IsDagPb()
        {
            var cid = ContentId.Parse("QmbFMke1KXqnYyBBWxB74N4c5SBnJMVAiMNRcGu6x1AwQH");

            Assert.True(ContentId.TryParseBinary(cid.ToBytes(), out var parsed));
            Assert.Equal(cid, parsed);
            Assert.Equal(34, cid.ToBytes().Length);
        }

        [Fact]
        public void Base58_RoundTripsLeadingZeros()
        {
            var bytes = new byte[] { 0, 0, 1, 2, 3 };

            Assert.True(Base58.TryDecode(Base58.Encode(bytes), out var decoded));
            Assert.Equal(bytes, decoded);
            Assert.Equal("11Ldp", Base58.Encode(bytes));
        }
    }
}
