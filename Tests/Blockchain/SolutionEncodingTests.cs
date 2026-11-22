using System.Numerics;
using MathChain.Blockchain.Certificates;
using MathChain.Blockchain.Common;
using MathChain.Blockchain.Solutions;
using MathChain.Domain.Entities;
using MathChain.Domain.Enums;
using MathChain.Domain.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;
using Nethereum.ABI.FunctionEncoding;
using Nethereum.Contracts;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.RPC.Eth.DTOs;

namespace MathChain.Tests.Blockchain
{
    public class SolutionEncodingTests
    {
        private const long ChainId = 84532;
        private const string Contract = "0x1111111111111111111111111111111111111111";
        private const string Account = "0x2222222222222222222222222222222222222222";
        private const string Token = "0x036CbD53842c5426634e7929541eC2318f3dCF7e";
        private const string Payer = "0x19e7e376e7c213b7e7e7e46cc70a5dd086daff2a";
        private const string Salt = "0xa05e334153147e75f3f416139b5109d1179cb56fef6a4ecb4c4cbc92a7c37b70";
        private const string SolutionId = "0x7aa520d2ae12ce76eeabc7fb578a5d1f1431237710fc844919c69764e8c1c213";
        private const string Nonce = "0x47aa1b0662580448d8dc598c16afa31e6656672ddb1d5038d97ca477873b3ad3";
        private const string BaseSepoliaUsdcDomain = "0x71f17a3b2ff373b803d70a5a07c046c1a2bc8e89c09ef722fcb047abe94c9818";
        private const string Digest = "0xedf9d4fc166c00fee5422b3b2ee80a04cd32becb53893ed0b3a779e8fceca93b";
        private const string Signature = "0xe34cdcd1334897e6a1cb8dd5f9310c123bd6bc95e288a03aa4d66249b68b2ca278eeb35d95f48988e85b8c0712174ad3f960892a4d15c6ba7f0e3d476f84eb901c";
        private const long ValidBefore = 1800000000;
        private const long Price = 3_000_000;
        private const string BuyCallData = "0x12c2128700000000000000000000000022222222222222222222222222222222222222227aa520d2ae12ce76eeabc7fb578a5d1f1431237710fc844919c69764e8c1c2130000000000000000000000000000000000000000000000000000000000000002a05e334153147e75f3f416139b5109d1179cb56fef6a4ecb4c4cbc92a7c37b7000000000000000000000000019e7e376e7c213b7e7e7e46cc70a5dd086daff2a00000000000000000000000000000000000000000000000000000000002dc6c00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000006b49d200000000000000000000000000000000000000000000000000000000000000001ce34cdcd1334897e6a1cb8dd5f9310c123bd6bc95e288a03aa4d66249b68b2ca278eeb35d95f48988e85b8c0712174ad3f960892a4d15c6ba7f0e3d476f84eb90";
        private const string SetPriceCallData = "0x6a00670b000000000000000000000000000000000000000000000000000000000000000100000000000000000000000000000000000000000000000000000000002625a0";
        private const string IsPurchasedCallData = "0xae74c7da00000000000000000000000022222222222222222222222222222222222222227aa520d2ae12ce76eeabc7fb578a5d1f1431237710fc844919c69764e8c1c213";
        private const string PricesResult = "0x00000000000000000000000000000000000000000000000000000000000f424000000000000000000000000000000000000000000000000000000000001e848000000000000000000000000000000000000000000000000000000000002dc6c0";
        private const string PurchasedTopic = "0x61353848b345ecbeeb17162e8f5bea365badd95fc3a20b3f68030ef43ff33534";
        private const string PurchasedData = "0x000000000000000000000000000000000000000000000000000000000000000200000000000000000000000000000000000000000000000000000000002dc6c0";

        private static readonly Guid FormulaId = Guid.Parse("b3ba5880-baaa-4172-8f9e-2bb240efddfd");

        private static SolutionPaymentRegistry Registry(string? relayerKey = null)
        {
            return new SolutionPaymentRegistry(new BlockchainSettings
            {
                RpcUrl = "http://127.0.0.1:1",
                ChainId = ChainId,
                SolutionsAddress = Contract,
                PaymentTokenAddress = Token,
                IssuerPrivateKey = relayerKey ?? string.Empty
            }, new PlatformTransactionLock(), NullLogger<SolutionPaymentRegistry>.Instance);
        }

        private static SolutionPaymentOrder Order()
        {
            return new SolutionPaymentOrder
            {
                AccountAddress = Account,
                SolutionId = SolutionId,
                Tier = SolutionTier.Hard,
                Salt = Salt,
                PayerAddress = Payer,
                PriceUnits = Price,
                ValidBefore = ValidBefore,
                Signature = Signature
            };
        }

        [Fact]
        public void ComputeSolutionId_MatchesTheSolidityEncoding()
        {
            Assert.Equal(SolutionId, SolutionEncoding.ComputeSolutionId(FormulaId, 4));
            Assert.NotEqual(SolutionId, SolutionEncoding.ComputeSolutionId(FormulaId, 5));
            Assert.Throws<ArgumentOutOfRangeException>(() => SolutionEncoding.ComputeSolutionId(FormulaId, 256));
            Assert.Throws<ArgumentOutOfRangeException>(() => SolutionEncoding.ComputeSolutionId(FormulaId, -1));
        }

        [Fact]
        public void ComputeNonce_MatchesTheContractPurchaseNonce()
        {
            Assert.Equal(Nonce, SolutionEncoding.ComputeNonce(ChainId, Contract, Account, SolutionId, SolutionTier.Hard, Salt));
            Assert.Equal(Nonce, Registry().ComputeNonce(Account, SolutionId, SolutionTier.Hard, Salt));
            Assert.NotEqual(Nonce, SolutionEncoding.ComputeNonce(ChainId, Contract, Account, SolutionId, SolutionTier.Easy, Salt));
            Assert.NotEqual(Nonce, SolutionEncoding.ComputeNonce(1, Contract, Account, SolutionId, SolutionTier.Hard, Salt));
        }

        [Fact]
        public void ComputeDomainSeparator_MatchesTheUsdcContractOnBaseSepolia()
        {
            Assert.Equal(BaseSepoliaUsdcDomain, SolutionEncoding.ComputeDomainSeparator("USDC", "2", ChainId, Token));
            Assert.NotEqual(BaseSepoliaUsdcDomain, SolutionEncoding.ComputeDomainSeparator("USD Coin", "2", ChainId, Token));
        }

        [Fact]
        public void ComputeAuthorizationDigest_MatchesTheTypedDataSignedByTheWallet()
        {
            var digest = SolutionEncoding.ComputeAuthorizationDigest(BaseSepoliaUsdcDomain, Payer, Contract, Price, 0, ValidBefore, Nonce);

            Assert.Equal(Digest, CertificateEncoding.ToHex(digest));
            Assert.Equal(Payer, SolutionEncoding.RecoverSigner(digest, Signature));
        }

        [Fact]
        public void IsSignedByPayer_AcceptsOnlyTheSignedOrder()
        {
            var registry = Registry();

            Assert.True(registry.IsSignedByPayer(Order()));

            var changes = new Action<SolutionPaymentOrder>[]
            {
                order => order.PriceUnits = 1_000_000,
                order => order.ValidBefore += 1,
                order => order.AccountAddress = "0x3333333333333333333333333333333333333333",
                order => order.Tier = SolutionTier.Easy,
                order => order.SolutionId = SolutionEncoding.ComputeSolutionId(FormulaId, 5),
                order => order.PayerAddress = "0x4444444444444444444444444444444444444444",
                order => order.Signature = "0x1234",
                order => order.Signature = string.Empty
            };

            foreach (var change in changes)
            {
                var order = Order();
                change(order);
                Assert.False(registry.IsSignedByPayer(order));
            }
        }

        [Theory]
        [InlineData("1c", 28)]
        [InlineData("01", 28)]
        [InlineData("1b", 27)]
        [InlineData("00", 27)]
        public void TrySplitSignature_NormalizesTheRecoveryId(string recoveryByte, int expectedV)
        {
            var signature = Signature[..^2] + recoveryByte;

            Assert.True(SolutionEncoding.TrySplitSignature(signature, out var v, out var r, out var s));
            Assert.Equal(expectedV, v);
            Assert.Equal(Signature.Substring(2, 64), Convert.ToHexString(r).ToLowerInvariant());
            Assert.Equal(Signature.Substring(66, 64), Convert.ToHexString(s).ToLowerInvariant());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("not hex")]
        [InlineData("0x1234")]
        public void TrySplitSignature_RejectsMalformedSignatures(string? signature)
        {
            Assert.False(SolutionEncoding.TrySplitSignature(signature, out _, out _, out _));
        }

        [Fact]
        public void TrySplitSignature_RejectsAnUnknownRecoveryId()
        {
            Assert.False(SolutionEncoding.TrySplitSignature(Signature[..^2] + "05", out _, out _, out _));
        }

        [Fact]
        public void BuyFunction_EncodesTheAuthorizationTuple()
        {
            SolutionEncoding.TrySplitSignature(Signature, out var v, out var r, out var s);
            var message = new BuySolutionFunction
            {
                Account = Account,
                SolutionId = SolutionId.HexToByteArray(),
                Tier = (byte)SolutionTier.Hard,
                Salt = Salt.HexToByteArray(),
                Authorization = new PaymentAuthorization
                {
                    Payer = Payer,
                    Value = new BigInteger(Price),
                    ValidAfter = BigInteger.Zero,
                    ValidBefore = new BigInteger(ValidBefore),
                    V = v,
                    R = r,
                    S = s
                }
            };

            Assert.Equal(BuyCallData, CertificateEncoding.ToHex(message.GetCallData()));
        }

        [Fact]
        public void BuildSetPriceCallData_MatchesTheContractAbi()
        {
            Assert.Equal(SetPriceCallData, Registry().BuildSetPriceCallData(SolutionTier.Medium, 2_500_000));
            Assert.Throws<BusinessRuleException>(() => Registry().BuildSetPriceCallData(SolutionTier.Medium, 0));
        }

        [Fact]
        public void IsPurchasedFunction_MatchesTheContractAbi()
        {
            var message = new IsSolutionPurchasedFunction { Account = Account, SolutionId = SolutionId.HexToByteArray() };

            Assert.Equal(IsPurchasedCallData, CertificateEncoding.ToHex(message.GetCallData()));
        }

        [Fact]
        public void PricesOutput_DecodesTheThreeTiers()
        {
            var prices = new FunctionCallDecoder().DecodeFunctionOutput<SolutionPricesOutput>(PricesResult);

            Assert.Equal(new BigInteger(1_000_000), prices.Easy);
            Assert.Equal(new BigInteger(2_000_000), prices.Medium);
            Assert.Equal(new BigInteger(3_000_000), prices.Hard);
        }

        [Fact]
        public void SolutionPurchasedEvent_DecodesTheContractLog()
        {
            Assert.Equal(PurchasedTopic, "0x" + EventExtensions.GetEventABI<SolutionPurchasedEvent>().Sha3Signature);

            var log = new FilterLog
            {
                Address = Contract,
                Topics = new object[]
                {
                    PurchasedTopic,
                    "0x0000000000000000000000002222222222222222222222222222222222222222",
                    SolutionId,
                    "0x00000000000000000000000019e7e376e7c213b7e7e7e46cc70a5dd086daff2a"
                },
                Data = PurchasedData
            };

            var decoded = Event<SolutionPurchasedEvent>.DecodeEvent(log).Event;

            Assert.Equal(Account, decoded.Account, ignoreCase: true);
            Assert.Equal(SolutionId, CertificateEncoding.ToHex(decoded.SolutionId));
            Assert.Equal(Payer, decoded.Payer, ignoreCase: true);
            Assert.Equal((byte)SolutionTier.Hard, decoded.Tier);
            Assert.Equal(new BigInteger(Price), decoded.Price);
        }

        [Theory]
        [InlineData("AlreadyPurchased(address,bytes32)", "0x77a670ee")]
        [InlineData("PriceMismatch(uint256,uint256)", "0x3ad7771a")]
        public void ErrorSelector_MatchesTheSolutionContractErrors(string signature, string expectedSelector)
        {
            Assert.Equal(expectedSelector, CertificateEncoding.ToHex(CertificateEncoding.ErrorSelector(signature)));
        }

        [Fact]
        public void Registry_WithoutAddresses_IsNotConfigured()
        {
            var registry = new SolutionPaymentRegistry(new BlockchainSettings { RpcUrl = "http://127.0.0.1:1", ChainId = ChainId },
                new PlatformTransactionLock(), NullLogger<SolutionPaymentRegistry>.Instance);

            Assert.False(registry.CanRead);
            Assert.False(registry.CanRelay);
            Assert.Throws<ServiceUnavailableException>(() => registry.ComputeNonce(Account, SolutionId, SolutionTier.Easy, Salt));
        }

        [Fact]
        public void Registry_WithTheIssuerKey_CanRelay()
        {
            var registry = Registry("0x" + new string('1', 64));

            Assert.True(registry.CanRelay);
            Assert.Equal(Payer, registry.Network.RelayerAddress, ignoreCase: true);
            Assert.False(Registry().CanRelay);
        }
    }
}
