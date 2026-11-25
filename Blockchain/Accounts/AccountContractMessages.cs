using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;

namespace MathChain.Blockchain.Accounts
{
    public class PackedUserOperation
    {
        [Parameter("address", "sender", 1)]
        public string Sender { get; set; } = string.Empty;

        [Parameter("uint256", "nonce", 2)]
        public BigInteger Nonce { get; set; }

        [Parameter("bytes", "initCode", 3)]
        public byte[] InitCode { get; set; } = Array.Empty<byte>();

        [Parameter("bytes", "callData", 4)]
        public byte[] CallData { get; set; } = Array.Empty<byte>();

        [Parameter("bytes32", "accountGasLimits", 5)]
        public byte[] AccountGasLimits { get; set; } = new byte[32];

        [Parameter("uint256", "preVerificationGas", 6)]
        public BigInteger PreVerificationGas { get; set; }

        [Parameter("bytes32", "gasFees", 7)]
        public byte[] GasFees { get; set; } = new byte[32];

        [Parameter("bytes", "paymasterAndData", 8)]
        public byte[] PaymasterAndData { get; set; } = Array.Empty<byte>();

        [Parameter("bytes", "signature", 9)]
        public byte[] Signature { get; set; } = Array.Empty<byte>();
    }

    [Function("handleOps")]
    public class HandleOpsFunction : FunctionMessage
    {
        [Parameter("tuple[]", "ops", 1)]
        public List<PackedUserOperation> Ops { get; set; } = new();

        [Parameter("address", "beneficiary", 2)]
        public string Beneficiary { get; set; } = string.Empty;
    }

    [Function("getNonce", "uint256")]
    public class GetNonceFunction : FunctionMessage
    {
        [Parameter("address", "sender", 1)]
        public string Sender { get; set; } = string.Empty;

        [Parameter("uint192", "key", 2)]
        public BigInteger Key { get; set; }
    }

    [Function("balanceOf", "uint256")]
    public class DepositBalanceFunction : FunctionMessage
    {
        [Parameter("address", "account", 1)]
        public string Account { get; set; } = string.Empty;
    }

    [Function("maxCostPerOperation", "uint256")]
    public class MaxCostPerOperationFunction : FunctionMessage
    {
    }

    [Function("accountAddress", "address")]
    public class AccountAddressFunction : FunctionMessage
    {
        [Parameter("address", "owner", 1)]
        public string Owner { get; set; } = string.Empty;

        [Parameter("uint256", "salt", 2)]
        public BigInteger Salt { get; set; }
    }

    [Function("createAccount", "address")]
    public class CreateAccountFunction : FunctionMessage
    {
        [Parameter("address", "owner", 1)]
        public string Owner { get; set; } = string.Empty;

        [Parameter("uint256", "salt", 2)]
        public BigInteger Salt { get; set; }
    }

    [Function("execute")]
    public class ExecuteFunction : FunctionMessage
    {
        [Parameter("address", "dest", 1)]
        public string Destination { get; set; } = string.Empty;

        [Parameter("uint256", "value", 2)]
        public BigInteger Value { get; set; }

        [Parameter("bytes", "func", 3)]
        public byte[] Data { get; set; } = Array.Empty<byte>();
    }

    [Function("claim", "uint256")]
    public class ClaimCertificateFunction : FunctionMessage
    {
        [Parameter("uint8", "kind", 1)]
        public byte Kind { get; set; }

        [Parameter("bytes32", "subjectId", 2)]
        public byte[] SubjectId { get; set; } = Array.Empty<byte>();

        [Parameter("bytes32", "dataHash", 3)]
        public byte[] DataHash { get; set; } = Array.Empty<byte>();

        [Parameter("uint256", "deadline", 4)]
        public BigInteger Deadline { get; set; }

        [Parameter("bytes", "signature", 5)]
        public byte[] Signature { get; set; } = Array.Empty<byte>();
    }

    [Function("revoke")]
    public class RevokeClaimedFunction : FunctionMessage
    {
        [Parameter("uint256", "tokenId", 1)]
        public BigInteger TokenId { get; set; }
    }

    [Function("deposit")]
    public class PaymasterDepositFunction : FunctionMessage
    {
    }

    [Event("UserOperationEvent")]
    public class UserOperationEvent : IEventDTO
    {
        [Parameter("bytes32", "userOpHash", 1, true)]
        public byte[] UserOpHash { get; set; } = Array.Empty<byte>();

        [Parameter("address", "sender", 2, true)]
        public string Sender { get; set; } = string.Empty;

        [Parameter("address", "paymaster", 3, true)]
        public string Paymaster { get; set; } = string.Empty;

        [Parameter("uint256", "nonce", 4, false)]
        public BigInteger Nonce { get; set; }

        [Parameter("bool", "success", 5, false)]
        public bool Success { get; set; }

        [Parameter("uint256", "actualGasCost", 6, false)]
        public BigInteger ActualGasCost { get; set; }

        [Parameter("uint256", "actualGasUsed", 7, false)]
        public BigInteger ActualGasUsed { get; set; }
    }

    [Event("UserOperationRevertReason")]
    public class UserOperationRevertReasonEvent : IEventDTO
    {
        [Parameter("bytes32", "userOpHash", 1, true)]
        public byte[] UserOpHash { get; set; } = Array.Empty<byte>();

        [Parameter("address", "sender", 2, true)]
        public string Sender { get; set; } = string.Empty;

        [Parameter("uint256", "nonce", 3, false)]
        public BigInteger Nonce { get; set; }

        [Parameter("bytes", "revertReason", 4, false)]
        public byte[] RevertReason { get; set; } = Array.Empty<byte>();
    }
}
