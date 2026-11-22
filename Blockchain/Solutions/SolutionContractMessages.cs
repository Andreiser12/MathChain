using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;

namespace MathChain.Blockchain.Solutions
{
    [Function("buy")]
    public class BuySolutionFunction : FunctionMessage
    {
        [Parameter("address", "account", 1)]
        public string Account { get; set; } = string.Empty;

        [Parameter("bytes32", "solutionId", 2)]
        public byte[] SolutionId { get; set; } = Array.Empty<byte>();

        [Parameter("uint8", "tier", 3)]
        public byte Tier { get; set; }

        [Parameter("bytes32", "salt", 4)]
        public byte[] Salt { get; set; } = Array.Empty<byte>();

        [Parameter("tuple", "authorization", 5)]
        public PaymentAuthorization Authorization { get; set; } = new();
    }

    public class PaymentAuthorization
    {
        [Parameter("address", "payer", 1)]
        public string Payer { get; set; } = string.Empty;

        [Parameter("uint256", "value", 2)]
        public BigInteger Value { get; set; }

        [Parameter("uint256", "validAfter", 3)]
        public BigInteger ValidAfter { get; set; }

        [Parameter("uint256", "validBefore", 4)]
        public BigInteger ValidBefore { get; set; }

        [Parameter("uint8", "v", 5)]
        public byte V { get; set; }

        [Parameter("bytes32", "r", 6)]
        public byte[] R { get; set; } = Array.Empty<byte>();

        [Parameter("bytes32", "s", 7)]
        public byte[] S { get; set; } = Array.Empty<byte>();
    }

    [Function("setPrice")]
    public class SetSolutionPriceFunction : FunctionMessage
    {
        [Parameter("uint8", "tier", 1)]
        public byte Tier { get; set; }

        [Parameter("uint256", "price", 2)]
        public BigInteger Price { get; set; }
    }

    [Function("prices", typeof(SolutionPricesOutput))]
    public class SolutionPricesFunction : FunctionMessage
    {
    }

    [FunctionOutput]
    public class SolutionPricesOutput : IFunctionOutputDTO
    {
        [Parameter("uint256", "easy", 1)]
        public BigInteger Easy { get; set; }

        [Parameter("uint256", "medium", 2)]
        public BigInteger Medium { get; set; }

        [Parameter("uint256", "hard", 3)]
        public BigInteger Hard { get; set; }
    }

    [Function("isPurchased", "bool")]
    public class IsSolutionPurchasedFunction : FunctionMessage
    {
        [Parameter("address", "account", 1)]
        public string Account { get; set; } = string.Empty;

        [Parameter("bytes32", "solutionId", 2)]
        public byte[] SolutionId { get; set; } = Array.Empty<byte>();
    }

    [Function("balanceOf", "uint256")]
    public class TokenBalanceOfFunction : FunctionMessage
    {
        [Parameter("address", "account", 1)]
        public string Account { get; set; } = string.Empty;
    }

    [Function("DOMAIN_SEPARATOR", "bytes32")]
    public class TokenDomainSeparatorFunction : FunctionMessage
    {
    }

    [Event("SolutionPurchased")]
    public class SolutionPurchasedEvent : IEventDTO
    {
        [Parameter("address", "account", 1, true)]
        public string Account { get; set; } = string.Empty;

        [Parameter("bytes32", "solutionId", 2, true)]
        public byte[] SolutionId { get; set; } = Array.Empty<byte>();

        [Parameter("address", "payer", 3, true)]
        public string Payer { get; set; } = string.Empty;

        [Parameter("uint8", "tier", 4, false)]
        public byte Tier { get; set; }

        [Parameter("uint256", "price", 5, false)]
        public BigInteger Price { get; set; }
    }
}
