using System.Numerics;
using System.Text;
using MathChain.Blockchain.Certificates;
using MathChain.Domain.Enums;
using Nethereum.ABI;
using Nethereum.Signer;
using Nethereum.Util;

namespace MathChain.Blockchain.Accounts
{
    public static class UserOperationEncoding
    {
        public const string ClaimType = "Claim(address student,uint8 kind,bytes32 subjectId,bytes32 dataHash,uint256 deadline)";
        public const string ClaimsDomainName = "MathChain Certificate Claims";
        public const string ClaimsDomainVersion = "1";

        private const int WordSizeInBytes = 32;
        private const int AddressSizeInBytes = 20;
        private const int GasFieldSizeInBytes = 16;
        private const string DomainType = "EIP712Domain(string name,string version,uint256 chainId,address verifyingContract)";

        public static byte[] PackUints(BigInteger high, BigInteger low)
        {
            var maximum = BigInteger.One << 128;

            if (high < 0 || low < 0 || high >= maximum || low >= maximum)
            {
                throw new ArgumentOutOfRangeException(nameof(high), "Packed gas values must fit in 128 bits.");
            }

            return ToWord((high << 128) | low);
        }

        public static byte[] ComputeUserOpHash(PackedUserOperation operation, string entryPointAddress, long chainId)
        {
            var packed = new ABIEncode().GetSha3ABIEncoded(
                new ABIValue("address", operation.Sender),
                new ABIValue("uint256", operation.Nonce),
                new ABIValue("bytes32", Keccak(operation.InitCode)),
                new ABIValue("bytes32", Keccak(operation.CallData)),
                new ABIValue("bytes32", operation.AccountGasLimits),
                new ABIValue("uint256", operation.PreVerificationGas),
                new ABIValue("bytes32", operation.GasFees),
                new ABIValue("bytes32", Keccak(operation.PaymasterAndData)));

            return new ABIEncode().GetSha3ABIEncoded(
                new ABIValue("bytes32", packed),
                new ABIValue("address", entryPointAddress),
                new ABIValue("uint256", new BigInteger(chainId)));
        }

        public static byte[] ComputeSponsorHash(PackedUserOperation operation, string paymasterAddress, long chainId, long validUntil, long validAfter)
        {
            if (operation.PaymasterAndData.Length < AddressSizeInBytes + 2 * GasFieldSizeInBytes)
            {
                throw new ArgumentException("The operation has no paymaster gas limits.", nameof(operation));
            }

            var paymasterGas = new BigInteger(operation.PaymasterAndData[AddressSizeInBytes..(AddressSizeInBytes + 2 * GasFieldSizeInBytes)],
                isUnsigned: true, isBigEndian: true);

            return new ABIEncode().GetSha3ABIEncoded(
                new ABIValue("address", operation.Sender),
                new ABIValue("uint256", operation.Nonce),
                new ABIValue("bytes32", Keccak(operation.InitCode)),
                new ABIValue("bytes32", Keccak(operation.CallData)),
                new ABIValue("bytes32", operation.AccountGasLimits),
                new ABIValue("uint256", paymasterGas),
                new ABIValue("uint256", operation.PreVerificationGas),
                new ABIValue("bytes32", operation.GasFees),
                new ABIValue("uint256", new BigInteger(chainId)),
                new ABIValue("address", paymasterAddress),
                new ABIValue("uint48", new BigInteger(validUntil)),
                new ABIValue("uint48", new BigInteger(validAfter)));
        }

        public static byte[] BuildPaymasterData(string paymasterAddress, BigInteger verificationGas, BigInteger postOpGas,
            long validUntil, long validAfter, byte[] signature)
        {
            var validity = new ABIEncode().GetABIEncoded(
                new ABIValue("uint48", new BigInteger(validUntil)),
                new ABIValue("uint48", new BigInteger(validAfter)));
            var gasLimits = PackUints(verificationGas, postOpGas);

            return AddressBytes(paymasterAddress)
                .Concat(gasLimits)
                .Concat(validity)
                .Concat(signature)
                .ToArray();
        }

        public static byte[] BuildInitCode(string factoryAddress, byte[] createAccountCallData)
        {
            return AddressBytes(factoryAddress).Concat(createAccountCallData).ToArray();
        }

        public static byte[] ComputeVoucherDigest(string claimsAddress, long chainId, string student, CertificateKind kind,
            byte[] subjectId, byte[] dataHash, long deadline)
        {
            var domainSeparator = new ABIEncode().GetSha3ABIEncoded(
                new ABIValue("bytes32", KeccakText(DomainType)),
                new ABIValue("bytes32", KeccakText(ClaimsDomainName)),
                new ABIValue("bytes32", KeccakText(ClaimsDomainVersion)),
                new ABIValue("uint256", new BigInteger(chainId)),
                new ABIValue("address", claimsAddress));
            var structHash = new ABIEncode().GetSha3ABIEncoded(
                new ABIValue("bytes32", KeccakText(ClaimType)),
                new ABIValue("address", student),
                new ABIValue("uint8", (int)kind),
                new ABIValue("bytes32", subjectId),
                new ABIValue("bytes32", dataHash),
                new ABIValue("uint256", new BigInteger(deadline)));

            var payload = new byte[2 + 2 * WordSizeInBytes];
            payload[0] = 0x19;
            payload[1] = 0x01;
            domainSeparator.CopyTo(payload, 2);
            structHash.CopyTo(payload, 2 + WordSizeInBytes);

            return Sha3Keccack.Current.CalculateHash(payload);
        }

        public static byte[] SignDigest(EthECKey key, byte[] digest)
        {
            var signature = key.SignAndCalculateV(digest);

            return signature.R.Concat(signature.S).Concat(signature.V).ToArray();
        }

        public static byte[] SignPersonal(EthECKey key, byte[] hash)
        {
            return Convert.FromHexString(new EthereumMessageSigner().Sign(hash, key)[2..]);
        }

        public static byte[] ToWord(BigInteger value)
        {
            var bytes = value.ToByteArray(isUnsigned: true, isBigEndian: true);

            if (bytes.Length > WordSizeInBytes)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            var word = new byte[WordSizeInBytes];
            bytes.CopyTo(word, WordSizeInBytes - bytes.Length);

            return word;
        }

        private static byte[] AddressBytes(string address)
        {
            var bytes = Convert.FromHexString(address.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? address[2..] : address);

            if (bytes.Length != AddressSizeInBytes)
            {
                throw new ArgumentException("The value is not an Ethereum address.", nameof(address));
            }

            return bytes;
        }

        private static byte[] Keccak(byte[] data)
        {
            return Sha3Keccack.Current.CalculateHash(data);
        }

        private static byte[] KeccakText(string text)
        {
            return Sha3Keccack.Current.CalculateHash(Encoding.UTF8.GetBytes(text));
        }
    }
}
