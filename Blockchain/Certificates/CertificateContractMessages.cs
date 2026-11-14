using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;

namespace MathChain.Blockchain.Certificates
{
    [Function("issueBatch", "uint256[]")]
    public class IssueBatchFunction : FunctionMessage
    {
        [Parameter("address[]", "students", 1)]
        public List<string> Students { get; set; } = new();

        [Parameter("uint8", "kind", 2)]
        public byte Kind { get; set; }

        [Parameter("bytes32", "subjectId", 3)]
        public byte[] SubjectId { get; set; } = Array.Empty<byte>();

        [Parameter("bytes32[]", "dataHashes", 4)]
        public List<byte[]> DataHashes { get; set; } = new();
    }

    [Function("revoke")]
    public class RevokeFunction : FunctionMessage
    {
        [Parameter("uint256", "tokenId", 1)]
        public BigInteger TokenId { get; set; }
    }

    [Function("grantRole")]
    public class GrantRoleFunction : FunctionMessage
    {
        [Parameter("bytes32", "role", 1)]
        public byte[] Role { get; set; } = Array.Empty<byte>();

        [Parameter("address", "account", 2)]
        public string Account { get; set; } = string.Empty;
    }

    [Function("hasRole", "bool")]
    public class HasRoleFunction : FunctionMessage
    {
        [Parameter("bytes32", "role", 1)]
        public byte[] Role { get; set; } = Array.Empty<byte>();

        [Parameter("address", "account", 2)]
        public string Account { get; set; } = string.Empty;
    }

    [Function("totalIssued", "uint256")]
    public class TotalIssuedFunction : FunctionMessage
    {
    }

    [Function("findCertificate", "uint256")]
    public class FindCertificateFunction : FunctionMessage
    {
        [Parameter("address", "student", 1)]
        public string Student { get; set; } = string.Empty;

        [Parameter("uint8", "kind", 2)]
        public byte Kind { get; set; }

        [Parameter("bytes32", "subjectId", 3)]
        public byte[] SubjectId { get; set; } = Array.Empty<byte>();
    }

    [Function("getCertificate", typeof(GetCertificateOutput))]
    public class GetCertificateFunction : FunctionMessage
    {
        [Parameter("uint256", "tokenId", 1)]
        public BigInteger TokenId { get; set; }
    }

    [FunctionOutput]
    public class GetCertificateOutput : IFunctionOutputDTO
    {
        [Parameter("tuple", "", 1)]
        public CertificateStruct Certificate { get; set; } = new();
    }

    public class CertificateStruct
    {
        [Parameter("address", "student", 1)]
        public string Student { get; set; } = string.Empty;

        [Parameter("uint64", "issuedAt", 2)]
        public ulong IssuedAt { get; set; }

        [Parameter("uint8", "kind", 3)]
        public byte Kind { get; set; }

        [Parameter("bool", "revoked", 4)]
        public bool Revoked { get; set; }

        [Parameter("address", "issuer", 5)]
        public string Issuer { get; set; } = string.Empty;

        [Parameter("bytes32", "subjectId", 6)]
        public byte[] SubjectId { get; set; } = Array.Empty<byte>();

        [Parameter("bytes32", "dataHash", 7)]
        public byte[] DataHash { get; set; } = Array.Empty<byte>();
    }

    [Event("CertificateIssued")]
    public class CertificateIssuedEvent : IEventDTO
    {
        [Parameter("uint256", "tokenId", 1, true)]
        public BigInteger TokenId { get; set; }

        [Parameter("address", "student", 2, true)]
        public string Student { get; set; } = string.Empty;

        [Parameter("address", "issuer", 3, true)]
        public string Issuer { get; set; } = string.Empty;

        [Parameter("uint8", "kind", 4, false)]
        public byte Kind { get; set; }

        [Parameter("bytes32", "subjectId", 5, false)]
        public byte[] SubjectId { get; set; } = Array.Empty<byte>();

        [Parameter("bytes32", "dataHash", 6, false)]
        public byte[] DataHash { get; set; } = Array.Empty<byte>();
    }
}
