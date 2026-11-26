using System.Text;
using MathChain.Infrastructure.Ipfs;

namespace MathChain.Tests.Infrastructure
{
    public class UnixFsDagTests
    {
        private static byte[] Content(int size)
        {
            var bytes = new byte[size];
            for (var i = 0; i < size; i++)
            {
                bytes[i] = (byte)((i * 31 + 7) % 251);
            }

            return bytes;
        }

        private static readonly string[] TwoLevelBlocks =
        {
            "04234261",
            "07264564",
            "122a0a240155122076be8b528d0075f7aae98d6fa57a6d3c83ae480a8469e668d7b0af968995ac71120018010a06080218012001",
            "122a0a2401551220b999f79c534a332dfb989ab78cda3d1967c16133ca1d668cf62737f8d768962f12001804122a0a24015512201e567080b2837b80f9d0e207dfce2f7171721294a41c2b80bcf09dc118a8350312001804122a0a24015512201c7155270705c237a17c0d3c18a849b81fbd2d22c93d35bbeb536246c24b1996120018040a0a0802180c200420042004",
            "122b0a2401701220b0de43d0c3f374cdb88ae2d8fb2e50572e13c8aa8182da40fc7373b09242b1601200189c01122a0a240170122072c6b58a15994edae96484fdc1bf6fd08854d204e1474d7ce36331341f52b1f7120018350a080802180d200c2001",
            "80",
            "83a2c1e0"
        };

        private static readonly string[] FullPlusOneBlocks =
        {
            "01203f5e",
            "04234261",
            "07264564",
            "122a0a240155122050e721e49c013f00c62cf59f2163542a9d8df02464efeb615d31051b0fddc326120018010a06080218012001",
            "122a0a24015512208cf519924b1d08932aac8a36f0248a9056536df46d694a2b38214427b61666b112001804122a0a24015512209c4a2f45eab80ebab17232cdc19ec1c8e874928b4954585dc3485eafa0eaecb712001804122a0a24015512200a8c8844c36062f8c2a43148496e7ca19af18400cd2ea1d896ea5955b3c1d1c1120018040a0a0802180c200420042004",
            "122a0a2401551220ad19383d4a4b191a68babbbca0516be31e3f010c921ed1d781b69b0ec77c1ef712001804122a0a2401551220015c29dcfbbffddf0a0092867dfe6ef057ce8000724e20a005c7cd7722a628a912001804122a0a24015512202eaa5d291266d5bc7f4fc431f11ec1919d2e146f4e4a1bded50373ad001e81dc120018040a0a0802180c200420042004",
            "122a0a2401551220b999f79c534a332dfb989ab78cda3d1967c16133ca1d668cf62737f8d768962f12001804122a0a24015512201e567080b2837b80f9d0e207dfce2f7171721294a41c2b80bcf09dc118a8350312001804122a0a24015512201c7155270705c237a17c0d3c18a849b81fbd2d22c93d35bbeb536246c24b1996120018040a0a0802180c200420042004",
            "122a0a24017012209ef3e427b02312078928b100a16854b5aaea795c2dfbf6f81a30473811ef19d7120018350a06080218012001",
            "122b0a24017012205d4d62410afffb313ee4cf3e16b09c3c0232cbdb8ca0d2ca8165c4a048ac8355120018e704122a0a2401701220b2e4bf506e3a11e0d40730e878b71f4472de5e3a5c1fd45197866c98513e7df9120018690a080802182520242001",
            "122b0a2401701220b0de43d0c3f374cdb88ae2d8fb2e50572e13c8aa8182da40fc7373b09242b1601200189c01122b0a24017012203f8651f7594f21923bdd4ec7aa4dba09d37594c74b207993ce7109cac0d6a82c1200189c01122b0a24017012206a8de9adead594ea6aeff0418c00df7198acc43d42332a2595807095f91f1e461200189c010a0a08021824200c200c200c",
            "77",
            "7a99b8d7",
            "7d9cbbda",
            "809fbedd",
            "83a2c1e0",
            "f61a3958",
            "f91d3c5b"
        };

        [Theory]
        [InlineData("empty", 0, 262144, 174, 1, "bafkreihdwdcefgh4dqkjv67uzcmw7ojee6xedzdetojuzjevtenxquvyku")]
        [InlineData("one-byte", 1, 262144, 174, 1, "bafkreigkgwdvr5wspzwpiutssn4xpj2i7wedshnwphhnu7ohx4pqaxxipe")]
        [InlineData("small", 1000, 262144, 174, 1, "bafkreiaaqve5st5hdz5ausb5qq4a2bnjeoslddtzxipyvdo2zerzk3js54")]
        [InlineData("chunk-minus-one", 262143, 262144, 174, 1, "bafkreiaakdbr3mk3i2fyatdvfsyktszyq74pjcglkrozlwyc4xyjg4bowi")]
        [InlineData("exact-chunk", 262144, 262144, 174, 1, "bafkreieiuj5mzeuqor24l4lmo2yumm6665yv4gq7ozzp2nrfmqrjyboosy")]
        [InlineData("chunk-plus-one", 262145, 262144, 174, 3, "bafybeich6cqd6yabw3cku6edwmz4rvwe275nx2l6ootvc2bpbt5bdvarua")]
        [InlineData("three-chunks", 524298, 262144, 174, 4, "bafybeie47ugxb6ive4whedv5sx7kgvsb4kpxqxvnxx547ck635s4sq3guq")]
        [InlineData("ten-megabytes", 10485760, 262144, 174, 41, "bafybeie64rm3zbbks4cbfdqetj73fycbh3cog2oja3mqv4lhhs7w23clyq")]
        [InlineData("tiny-tree-two-levels", 13, 4, 3, 7, "bafybeibplwbu42y5y7u6ggepcgqbt4dn65kptxkpqho2ki3dewpmlahg7q")]
        [InlineData("tiny-tree-three-levels", 50, 4, 3, 21, "bafybeieh7clgyomf7jydlst2tkvmkscvapdneblojm6yqato654bgpuzdq")]
        [InlineData("tiny-tree-full", 36, 4, 3, 13, "bafybeic5jvreccx77myt5zgphyllbhb4aizmxw4mudjmvalfysqerledku")]
        [InlineData("tiny-tree-full-plus-one", 37, 4, 3, 17, "bafybeidoykhqzrhayhpqncuk7vfsxcfft5hpeekjzh5cmijkclhalknlli")]
        public void Build_MatchesTheReferenceImplementation(string name, int size, int chunkSize, int maximumChildren, int blockCount, string root)
        {
            var file = UnixFsDag.Build(Content(size), chunkSize, maximumChildren);

            Assert.Equal(root, file.Root.ToString());
            Assert.Equal(blockCount, file.Blocks.Count);
            Assert.False(string.IsNullOrEmpty(name));
        }

        [Fact]
        public void Build_Text_MatchesTheReferenceImplementation()
        {
            var file = UnixFsDag.Build(Encoding.UTF8.GetBytes("MathChain curriculum\n"));

            Assert.Equal("bafkreih77q4xcux2oixp2lq5mm5ithbcxniucznvyeeqxp3wxiacksuyya", file.Root.ToString());
        }

        [Fact]
        public void Build_TwoLevels_ProducesTheSameBlocksByteForByte()
        {
            var file = UnixFsDag.Build(Content(13), 4, 3);

            Assert.Equal(TwoLevelBlocks, file.Blocks.Select(b => Convert.ToHexString(b.Bytes).ToLowerInvariant()).OrderBy(h => h, StringComparer.Ordinal));
        }

        [Fact]
        public void Build_PartialLastGroup_ProducesTheSameBlocksByteForByte()
        {
            var file = UnixFsDag.Build(Content(37), 4, 3);

            Assert.Equal(FullPlusOneBlocks, file.Blocks.Select(b => Convert.ToHexString(b.Bytes).ToLowerInvariant()).OrderBy(h => h, StringComparer.Ordinal));
        }

        private static readonly string[] VersionZeroFullPlusOneBlocks =
        {
            "0a0708021201771801",
            "0a0a0802120401203f5e1804",
            "0a0a08021204042342611804",
            "0a0a08021204072645641804",
            "0a0a080212047a99b8d71804",
            "0a0a080212047d9cbbda1804",
            "0a0a08021204809fbedd1804",
            "0a0a0802120483a2c1e01804",
            "0a0a08021204f61a39581804",
            "0a0a08021204f91d3c5b1804",
            "12280a221220232cdb4b1aab3e90202cc6ed28cdbc036d07993521817dc3d3f36ba1bb1d8f941200180c12280a221220ba748f45646f532343fcc646484e8cea695deef9a1db9562ce47fa4209cd0ee41200180c12280a221220eeca6fafbe967b079a8f9fe9228b7872af01165e3a4b97a5c69360856d11cabc1200180c0a0a0802180c200420042004",
            "12280a221220533eb3bda18e17554f19d02b267d61cf5cdf39087652e1e39e7ac0fdb04219461200183b0a06080218012001",
            "12280a2212208047a874a29c6f24dee91c2d6d80138933350fefc700665e41201d09f0f214b41200180c12280a221220ded92e2fe68f0a569879daef224591b0d60410dc267ae8a4b54d2c1b2cad9bff1200180c12280a2212201ff5ed47ab63809bf25d9820ff5299ba7cd6e95abf8bdbc1cb07400458712c411200180c0a0a0802180c200420042004",
            "12280a221220b0135709fbf2c1d09f90911def83c75f0c72d4e376ce0f3448f796d289a05526120018090a06080218012001",
            "12280a221220e653ca9bf314dc94cf6efbf0239f50622fce7bbae5d0ab1834063c419f62ccd61200180c12280a2212203df210252f8ac64a0c542f232c4cee78c01177fedccf6d8d7c6c95759d8535a21200180c12280a2212201f34bd8dc6ae9b920ff31ec9a472f1303cb32a4c806799fe822f495cd68504241200180c0a0a0802180c200420042004",
            "12290a221220b027dbc6dd48656b2c56d01a339a94b3a0a126c0019bfc66fdddc7109e290391120018ae0112290a22122007d990491bc2af77a6fc6e57109bcac1bf0b9542155e9e5ab197e1b7f317b188120018ae0112290a221220870048550288b77302439875a013824348c50e02c5ce8b7b9fdbd167dee6430c120018ae010a0a08021824200c200c200c",
            "12290a221220e898158a220745167bf11918c7ab7b3ceff6e7fa50a83585c6dc4a272f7ec0be120018970512280a221220b64ad2329e023fd63bd144a36c1730d177a3ebefc0134613df00c7cde7d8f34e1200186d0a080802182520242001"
        };

        [Theory]
        [InlineData(2UL, 0, 262144, 174, 1, "QmbFMke1KXqnYyBBWxB74N4c5SBnJMVAiMNRcGu6x1AwQH")]
        [InlineData(2UL, 1, 262144, 174, 1, "QmXC9WjuLo7EfydBdbWyvpGif62nrxbDCX2vcz9ZqLTQvV")]
        [InlineData(2UL, 1000, 262144, 174, 1, "Qmeg4t1d1Smno7xVHW3AWmtmjU3ouXvCiLegmHdQztLGmY")]
        [InlineData(2UL, 262145, 262144, 174, 3, "QmP8yTqWQcb3qVoBcdE4pTUzxh9kazndg5ELrbXBmdj17M")]
        [InlineData(2UL, 600000, 262144, 174, 4, "QmXoNkP3DBcSVpFSPw5WZrxs2LvbKDGtvQSmhxNXYYVtwJ")]
        [InlineData(2UL, 13, 4, 3, 7, "QmWLBnTZPDVT9pJsrb5N5GnhoC4PmcRNYYxQJNFygyuYBF")]
        [InlineData(2UL, 37, 4, 3, 17, "QmSBrxRCu8AjRinSB97zvTzpY3ZwZXXXMeixZ37zA6p86p")]
        [InlineData(0UL, 0, 262144, 174, 1, "Qmdsf68UUYTSSx3i4GtDJfxzpAEZt7Mp23m3qa36LYMSiW")]
        [InlineData(0UL, 1, 262144, 174, 1, "QmehBbQvNiTNznfdqXSkVtzp8NkaDPW9b5BXfQXHepqq8W")]
        [InlineData(0UL, 1000, 262144, 174, 1, "QmaBk6hyidArcpHDUTdoQagZf7dWT4pQVjQhpKndBB7quF")]
        [InlineData(0UL, 262145, 262144, 174, 3, "QmVWY4Ank69rPVZxJZjvbPkbynznV2qL6fzbd3zDJrSXAB")]
        [InlineData(0UL, 600000, 262144, 174, 4, "QmZKHh6Tx6USLZX5aNBmLpVA7hYA1qo9bHbMVfsgAWfz66")]
        [InlineData(0UL, 13, 4, 3, 7, "Qmf6L3Zms7brtfEBLM1Myc2o2RA3YD7UYi3bp8LtEAekUc")]
        [InlineData(0UL, 37, 4, 3, 17, "QmUWE4mTuBKa5qTMZymxmR1wfnP8t5pnrvUstcqfrazvsX")]
        public void BuildVersionZero_MatchesTheReferenceImplementation(ulong leafType, int size, int chunkSize, int maximumChildren, int blockCount, string root)
        {
            var file = UnixFsDag.BuildVersionZero(Content(size), leafType, chunkSize, maximumChildren);

            Assert.Equal(root, file.Root.ToString());
            Assert.Equal(blockCount, file.Blocks.Count);
        }

        [Fact]
        public void BuildVersionZero_ProducesTheSameBlocksByteForByte()
        {
            var file = UnixFsDag.BuildVersionZero(Content(37), UnixFsData.FileType, 4, 3);

            Assert.Equal(VersionZeroFullPlusOneBlocks, file.Blocks.Select(b => Convert.ToHexString(b.Bytes).ToLowerInvariant()).OrderBy(h => h, StringComparer.Ordinal));
        }

        [Theory]
        [InlineData(2UL, 600000)]
        [InlineData(0UL, 600000)]
        [InlineData(2UL, 1000)]
        public async Task ReadAsync_VersionZeroDag_RebuildsTheOriginalContent(ulong leafType, int size)
        {
            var content = Content(size);
            var file = UnixFsDag.BuildVersionZero(content, leafType);
            var store = file.Blocks.ToDictionary(b => b.Cid, b => b.Bytes);

            var read = await UnixFsDag.ReadAsync(file.Root, cids => Task.FromResult<IReadOnlyDictionary<ContentId, byte[]>>(
                cids.Where(store.ContainsKey).ToDictionary(c => c, c => store[c])), long.MaxValue);

            Assert.Equal(content, read);
        }

        [Theory]
        [InlineData(0, 262144, 174)]
        [InlineData(1, 262144, 174)]
        [InlineData(524298, 262144, 174)]
        [InlineData(50, 4, 3)]
        [InlineData(37, 4, 3)]
        public async Task ReadAsync_RebuildsTheOriginalContent(int size, int chunkSize, int maximumChildren)
        {
            var content = Content(size);
            var file = UnixFsDag.Build(content, chunkSize, maximumChildren);
            var store = file.Blocks.ToDictionary(b => b.Cid, b => b.Bytes);

            var read = await UnixFsDag.ReadAsync(file.Root, cids => Task.FromResult<IReadOnlyDictionary<ContentId, byte[]>>(
                cids.Where(store.ContainsKey).ToDictionary(c => c, c => store[c])), long.MaxValue);

            Assert.Equal(content, read);
        }

        [Fact]
        public async Task ReadAsync_TamperedLeaf_IsRejected()
        {
            var file = UnixFsDag.Build(Content(50), 4, 3);
            var store = file.Blocks.ToDictionary(b => b.Cid, b => (byte[])b.Bytes.Clone());
            var leaf = file.Blocks.First(b => b.Cid.Codec == ContentCodec.Raw).Cid;
            store[leaf][0] ^= 0x01;

            await Assert.ThrowsAsync<InvalidDataException>(() => UnixFsDag.ReadAsync(file.Root, cids =>
                Task.FromResult<IReadOnlyDictionary<ContentId, byte[]>>(cids.ToDictionary(c => c, c => store[c])), long.MaxValue));
        }

        [Fact]
        public async Task ReadAsync_MissingBlock_IsRejected()
        {
            var file = UnixFsDag.Build(Content(50), 4, 3);
            var store = file.Blocks.Skip(1).ToDictionary(b => b.Cid, b => b.Bytes);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => UnixFsDag.ReadAsync(file.Root, cids =>
                Task.FromResult<IReadOnlyDictionary<ContentId, byte[]>>(cids.Where(store.ContainsKey).ToDictionary(c => c, c => store[c])), long.MaxValue));
        }

        [Fact]
        public async Task ReadAsync_LargerThanTheLimit_IsRejected()
        {
            var file = UnixFsDag.Build(Content(50), 4, 3);
            var store = file.Blocks.ToDictionary(b => b.Cid, b => b.Bytes);

            await Assert.ThrowsAsync<InvalidDataException>(() => UnixFsDag.ReadAsync(file.Root, cids =>
                Task.FromResult<IReadOnlyDictionary<ContentId, byte[]>>(cids.ToDictionary(c => c, c => store[c])), 49));
        }
    }
}
