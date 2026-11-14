using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using MathChain.Domain.Entities;
using Nethereum.ABI;
using Nethereum.Contracts;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Util;

namespace MathChain.Blockchain.Certificates
{
    public static class CertificateEncoding
    {
        private const int WordSizeInBytes = 32;
        private const int GuidSizeInBytes = 16;
        private const int MaximumGrade = ushort.MaxValue;

        private static readonly Regex Bytes32Pattern = new("^0x[0-9a-fA-F]{64}$", RegexOptions.Compiled);

        public static byte[] IssuerRole { get; } = Sha3Keccack.Current.CalculateHash(Encoding.UTF8.GetBytes("ISSUER_ROLE"));

        public static byte[] ToSubjectBytes(Guid subjectId)
        {
            var bytes = new byte[WordSizeInBytes];
            Convert.FromHexString(subjectId.ToString("N")).CopyTo(bytes, WordSizeInBytes - GuidSizeInBytes);
            return bytes;
        }

        public static Guid ToSubjectGuid(byte[] subjectBytes)
        {
            if (subjectBytes.Length != WordSizeInBytes)
            {
                throw new ArgumentException("A subject identifier must have 32 bytes.", nameof(subjectBytes));
            }

            return Guid.ParseExact(Convert.ToHexString(subjectBytes, WordSizeInBytes - GuidSizeInBytes, GuidSizeInBytes), "N");
        }

        public static string CreateSalt()
        {
            return ToHex(RandomNumberGenerator.GetBytes(WordSizeInBytes));
        }

        public static bool IsBytes32(string? value)
        {
            return value != null && Bytes32Pattern.IsMatch(value);
        }

        public static string ToHex(byte[] bytes)
        {
            return "0x" + Convert.ToHexString(bytes).ToLowerInvariant();
        }

        public static string ComputeDataHash(CertificateCommitment commitment)
        {
            if (!IsBytes32(commitment.Salt))
            {
                throw new ArgumentException("The certificate salt must be a 32 byte hexadecimal value.", nameof(commitment));
            }

            if (commitment.Grade < 0 || commitment.Grade > MaximumGrade)
            {
                throw new ArgumentException("The certificate grade is out of range.", nameof(commitment));
            }

            if (!AddressUtil.Current.IsValidEthereumAddressHexFormat(commitment.StudentAddress)
                || !AddressUtil.Current.IsValidEthereumAddressHexFormat(commitment.TeacherAddress))
            {
                throw new ArgumentException("The certificate addresses must be Ethereum addresses.", nameof(commitment));
            }

            var hash = new ABIEncode().GetSha3ABIEncoded(
                new ABIValue("address", commitment.StudentAddress),
                new ABIValue("uint8", (int)commitment.Kind),
                new ABIValue("bytes32", ToSubjectBytes(commitment.SubjectId)),
                new ABIValue("uint16", commitment.Grade),
                new ABIValue("address", commitment.TeacherAddress),
                new ABIValue("bytes32", commitment.Salt.HexToByteArray()));

            return ToHex(hash);
        }

        public static string BuildGrantIssuerRoleCallData(string issuerAddress)
        {
            var message = new GrantRoleFunction { Role = IssuerRole, Account = issuerAddress };

            return ToHex(message.GetCallData());
        }

        public static byte[] ErrorSelector(string signature)
        {
            return Sha3Keccack.Current.CalculateHash(Encoding.UTF8.GetBytes(signature)).Take(4).ToArray();
        }
    }
}
