using System.Numerics;
using System.Text;
using MathChain.Blockchain.Certificates;
using MathChain.Domain.Enums;
using Nethereum.ABI;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Signer;
using Nethereum.Util;

namespace MathChain.Blockchain.Solutions
{
    public static class SolutionEncoding
    {
        public const string DomainType = "EIP712Domain(string name,string version,uint256 chainId,address verifyingContract)";
        public const string ReceiveWithAuthorizationType =
            "ReceiveWithAuthorization(address from,address to,uint256 value,uint256 validAfter,uint256 validBefore,bytes32 nonce)";

        private const int SignatureSizeInBytes = 65;
        private const int WordSizeInBytes = 32;

        public static string ComputeSolutionId(Guid formulaId, int exerciseIndex)
        {
            if (exerciseIndex < 0 || exerciseIndex > byte.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(exerciseIndex));
            }

            var hash = new ABIEncode().GetSha3ABIEncoded(
                new ABIValue("bytes16", Convert.FromHexString(formulaId.ToString("N"))),
                new ABIValue("uint8", exerciseIndex));

            return CertificateEncoding.ToHex(hash);
        }

        public static string ComputeNonce(long chainId, string contractAddress, string accountAddress, string solutionId, SolutionTier tier, string salt)
        {
            var hash = new ABIEncode().GetSha3ABIEncoded(
                new ABIValue("uint256", new BigInteger(chainId)),
                new ABIValue("address", contractAddress),
                new ABIValue("address", accountAddress),
                new ABIValue("bytes32", RequireBytes32(solutionId)),
                new ABIValue("uint8", (int)tier),
                new ABIValue("bytes32", RequireBytes32(salt)));

            return CertificateEncoding.ToHex(hash);
        }

        public static string ComputeDomainSeparator(string name, string version, long chainId, string tokenAddress)
        {
            var hash = new ABIEncode().GetSha3ABIEncoded(
                new ABIValue("bytes32", Keccak(DomainType)),
                new ABIValue("bytes32", Keccak(name)),
                new ABIValue("bytes32", Keccak(version)),
                new ABIValue("uint256", new BigInteger(chainId)),
                new ABIValue("address", tokenAddress));

            return CertificateEncoding.ToHex(hash);
        }

        public static byte[] ComputeAuthorizationDigest(string domainSeparator, string from, string to, long value,
            long validAfter, long validBefore, string nonce)
        {
            var structHash = new ABIEncode().GetSha3ABIEncoded(
                new ABIValue("bytes32", Keccak(ReceiveWithAuthorizationType)),
                new ABIValue("address", from),
                new ABIValue("address", to),
                new ABIValue("uint256", new BigInteger(value)),
                new ABIValue("uint256", new BigInteger(validAfter)),
                new ABIValue("uint256", new BigInteger(validBefore)),
                new ABIValue("bytes32", RequireBytes32(nonce)));

            var payload = new byte[2 + WordSizeInBytes * 2];
            payload[0] = 0x19;
            payload[1] = 0x01;
            RequireBytes32(domainSeparator).CopyTo(payload, 2);
            structHash.CopyTo(payload, 2 + WordSizeInBytes);

            return Sha3Keccack.Current.CalculateHash(payload);
        }

        public static bool TrySplitSignature(string? signature, out byte v, out byte[] r, out byte[] s)
        {
            v = 0;
            r = Array.Empty<byte>();
            s = Array.Empty<byte>();

            if (string.IsNullOrWhiteSpace(signature))
            {
                return false;
            }

            byte[] bytes;

            try
            {
                bytes = signature.Trim().HexToByteArray();
            }
            catch (FormatException)
            {
                return false;
            }

            if (bytes.Length != SignatureSizeInBytes)
            {
                return false;
            }

            v = bytes[64] < 27 ? (byte)(bytes[64] + 27) : bytes[64];

            if (v != 27 && v != 28)
            {
                return false;
            }

            r = bytes[..32];
            s = bytes[32..64];

            return true;
        }

        public static string? RecoverSigner(byte[] digest, string signature)
        {
            if (!TrySplitSignature(signature, out var v, out var r, out var s))
            {
                return null;
            }

            try
            {
                var key = EthECKey.RecoverFromSignature(EthECDSASignatureFactory.FromComponents(r, s, v), digest);

                return key.GetPublicAddress().ToLowerInvariant();
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static byte[] RequireBytes32(string value)
        {
            if (!CertificateEncoding.IsBytes32(value))
            {
                throw new ArgumentException("The value must be a 32 byte hexadecimal string.", nameof(value));
            }

            return value.HexToByteArray();
        }

        private static byte[] Keccak(string text)
        {
            return Sha3Keccack.Current.CalculateHash(Encoding.UTF8.GetBytes(text));
        }
    }
}
