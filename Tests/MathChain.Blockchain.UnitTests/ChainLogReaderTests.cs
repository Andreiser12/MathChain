using System.Numerics;
using MathChain.Blockchain.Certificates;
using MathChain.Blockchain.Curriculum;
using MathChain.Blockchain.Indexing;
using MathChain.Blockchain.Solutions;
using MathChain.Domain.Entities;
using MathChain.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Nethereum.ABI;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;

namespace MathChain.Blockchain.UnitTests
{
    public class ChainLogReaderTests
    {
        private const string Certificates = "0x1b09a2e38ef9bfc02bb4837c01cff1533652a882";
        private const string Solutions = "0xad11198771d47190c973f474bf5d37065300cd2b";
        private const string Factory = "0x55f77452890e9e6da93fe7b09513acb021a9ce99";
        private const string Claims = "0x5d0f9d686861fd7c9cfef78245ef8607cb345afa";
        private const string Registry = "0xfb7586c2e3950fdb3798193fbf499fde33bb051d";
        private const string Student = "0x3333333333333333333333333333333333333333";
        private const string Issuer = "0x70997970c51812dc3a010c7d01b50e0d17dc79c8";

        private readonly ChainLogReader _reader = new(new BlockchainSettings
        {
            RpcUrl = "http://127.0.0.1:8545",
            ChainId = 84532,
            CertificatesAddress = Certificates,
            SolutionsAddress = Solutions,
            AccountFactoryAddress = Factory,
            CertificateClaimsAddress = Claims,
            CurriculumRegistryAddress = Registry
        }, NullLogger<ChainLogReader>.Instance);

        private static string Word(string type, object value)
        {
            return new ABIEncode().GetABIEncoded(new ABIValue(type, value)).ToHex(true);
        }

        private static FilterLog Log(string address, string[] topics, byte[] data, long block = 63000000, int index = 2)
        {
            return new FilterLog
            {
                Address = address,
                Topics = topics.Cast<object>().ToArray(),
                Data = data.ToHex(true),
                BlockNumber = new HexBigInteger(block),
                LogIndex = new HexBigInteger(index),
                TransactionHash = "0x" + new string('a', 64)
            };
        }

        [Theory]
        [InlineData("CertificateIssued", "0xf7cd537a75849d589ff4830e697d0ca628976ca041e8f8407d2ca6d14338f1ac")]
        [InlineData("CertificateRevoked", "0x325c62e70cfaa61cf0709d9607ea9d8451b235297c4b47c65ff6a6449c9faca4")]
        [InlineData("CertificateClaimed", "0x9ab076b104580483fe1391038ee7f2020922fd899addf3731b2905ba1de068cf")]
        [InlineData("AccountCreated", "0x33310a89c32d8cc00057ad6ef6274d2f8fe22389a992cf89983e09fc84f6cfff")]
        [InlineData("SolutionPurchased", "0x61353848b345ecbeeb17162e8f5bea365badd95fc3a20b3f68030ef43ff33534")]
        [InlineData("PriceChanged", "0x884cf27d975be4e0d670dacaac663443178b41024dabddd43ab6aac563325f2e")]
        [InlineData("CurriculumPublished", "0x26f12d0f02b837eea36c6c7b37618a98c0e1f7772cde29ef969e809258a668c2")]
        public void Topic_ShouldMatchTheSoliditySignature_WhenItIsComputedForAnIndexedEvent(string name, string expected)
        {
            var topic = name switch
            {
                "CertificateIssued" => ChainLogReader.Topic<CertificateIssuedEvent>(),
                "CertificateRevoked" => ChainLogReader.Topic<CertificateRevokedEvent>(),
                "CertificateClaimed" => ChainLogReader.Topic<CertificateClaimedEvent>(),
                "AccountCreated" => ChainLogReader.Topic<AccountCreatedEvent>(),
                "SolutionPurchased" => ChainLogReader.Topic<SolutionPurchasedEvent>(),
                "PriceChanged" => ChainLogReader.Topic<PriceChangedEvent>(),
                _ => ChainLogReader.Topic<CurriculumPublishedEvent>()
            };

            Assert.Equal(expected, topic);
        }

        [Fact]
        public void Decode_ShouldReadTheIndexedAndDataFields_WhenTheLogIsCertificateIssued()
        {
            var subject = Enumerable.Repeat((byte)0x11, 32).ToArray();
            var dataHash = Enumerable.Repeat((byte)0x22, 32).ToArray();
            var log = Log(Certificates,
                new[] { ChainLogReader.Topic<CertificateIssuedEvent>(), Word("uint256", 7), Word("address", Student), Word("address", Issuer) },
                new ABIEncode().GetABIEncoded(new ABIValue("uint8", 1), new ABIValue("bytes32", subject), new ABIValue("bytes32", dataHash)));

            var decoded = Assert.IsType<CertificateIssuedLog>(_reader.Decode(log));

            Assert.Equal(7, decoded.TokenId);
            Assert.Equal(Student, decoded.Student);
            Assert.Equal(Issuer, decoded.Issuer);
            Assert.Equal(CertificateKind.Chapter, decoded.Kind);
            Assert.Equal("0x" + new string('1', 64), decoded.SubjectId);
            Assert.Equal("0x" + new string('2', 64), decoded.DataHash);
            Assert.Equal(63000000L, decoded.BlockNumber);
            Assert.Equal(2, decoded.LogIndex);
            Assert.Equal(Certificates, decoded.Contract);
        }

        [Fact]
        public void Decode_ShouldReadTheTier_WhenPriceChangedHasAnIndexedEnum()
        {
            var log = Log(Solutions, new[] { ChainLogReader.Topic<PriceChangedEvent>(), Word("uint8", 2) },
                new ABIEncode().GetABIEncoded(new ABIValue("uint256", new BigInteger(3000000))));

            var decoded = Assert.IsType<PriceChangedLog>(_reader.Decode(log));

            Assert.Equal(2, decoded.Tier);
            Assert.Equal(3000000m, decoded.Price);
        }

        [Fact]
        public void Decode_ShouldReadTheDynamicBytes_WhenTheLogIsCurriculumPublished()
        {
            var cid = new byte[] { 0x01, 0x55, 0x12, 0x20 }.Concat(Enumerable.Repeat((byte)0xab, 32)).ToArray();
            var log = Log(Registry, new[] { ChainLogReader.Topic<CurriculumPublishedEvent>(), Word("uint256", 3), Word("address", Issuer) },
                new ABIEncode().GetABIEncoded(new ABIValue("bytes", cid), new ABIValue("uint64", 1764000000UL)));

            var decoded = Assert.IsType<CurriculumPublishedLog>(_reader.Decode(log));

            Assert.Equal(3, decoded.Version);
            Assert.Equal(cid, decoded.Cid);
            Assert.Equal(Issuer, decoded.Publisher);
        }

        [Fact]
        public void Decode_ShouldIgnoreTheLog_WhenTheEventComesFromAnotherContract()
        {
            var log = Log("0x9999999999999999999999999999999999999999",
                new[] { ChainLogReader.Topic<CertificateRevokedEvent>(), Word("uint256", 7), Word("address", Issuer) }, Array.Empty<byte>());

            Assert.Null(_reader.Decode(log));
        }

        [Fact]
        public void Decode_ShouldIgnoreTheLog_WhenTheContractDoesNotEmitThatEvent()
        {
            var log = Log(Solutions, new[] { ChainLogReader.Topic<CertificateRevokedEvent>(), Word("uint256", 7), Word("address", Issuer) }, Array.Empty<byte>());

            Assert.Null(_reader.Decode(log));
        }

        [Fact]
        public void Decode_ShouldSkipTheLog_WhenTheDataIsMalformed()
        {
            var log = Log(Certificates,
                new[] { ChainLogReader.Topic<CertificateIssuedEvent>(), Word("uint256", 7), Word("address", Student), Word("address", Issuer) }, new byte[] { 1, 2, 3 });

            Assert.Null(_reader.Decode(log));
        }

        [Fact]
        public void IsRangeError_ShouldReturnTrue_WhenANodeRefusesALargeRange()
        {
            var jsonRpc = new Nethereum.JsonRpc.Client.RpcResponseException(new Nethereum.JsonRpc.Client.RpcError(-32614, "eth_getLogs is limited to a 200 range"));
            var payload = new Nethereum.JsonRpc.Client.RpcClientUnknownException("Error occurred when trying to send rpc requests(s): eth_getLogs",
                new HttpRequestException("Response status code does not indicate success: 413 (Payload Too Large).", null, System.Net.HttpStatusCode.RequestEntityTooLarge));
            var results = new Nethereum.JsonRpc.Client.RpcResponseException(new Nethereum.JsonRpc.Client.RpcError(-32005, "query returned more than 10000 results"));

            Assert.True(ChainLogReader.IsRangeError(jsonRpc));
            Assert.True(ChainLogReader.IsRangeError(payload));
            Assert.True(ChainLogReader.IsRangeError(results));
        }

        [Fact]
        public void IsRangeError_ShouldReturnFalse_WhenTheFailureIsNotAboutTheRange()
        {
            var timeout = new Nethereum.JsonRpc.Client.RpcClientUnknownException("Error occurred when trying to send rpc requests(s): eth_getLogs",
                new HttpRequestException("Response status code does not indicate success: 503 (Service Unavailable).", null, System.Net.HttpStatusCode.ServiceUnavailable));

            Assert.False(ChainLogReader.IsRangeError(timeout));
            Assert.False(ChainLogReader.IsRangeError(new TaskCanceledException("The request was canceled due to the configured HttpClient.Timeout")));
        }

        [Fact]
        public void Constructor_ShouldNotBeConfigured_WhenThereAreNoContracts()
        {
            var reader = new ChainLogReader(new BlockchainSettings { RpcUrl = "http://127.0.0.1:8545", ChainId = 1 }, NullLogger<ChainLogReader>.Instance);

            Assert.False(reader.IsConfigured);
            Assert.True(_reader.IsConfigured);
        }
    }
}
