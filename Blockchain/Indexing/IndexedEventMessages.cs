using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;

namespace MathChain.Blockchain.Indexing
{
    [Event("CertificateRevoked")]
    public class CertificateRevokedEvent : IEventDTO
    {
        [Parameter("uint256", "tokenId", 1, true)]
        public BigInteger TokenId { get; set; }

        [Parameter("address", "revokedBy", 2, true)]
        public string RevokedBy { get; set; } = string.Empty;
    }

    [Event("CertificateClaimed")]
    public class CertificateClaimedEvent : IEventDTO
    {
        [Parameter("uint256", "tokenId", 1, true)]
        public BigInteger TokenId { get; set; }

        [Parameter("address", "student", 2, true)]
        public string Student { get; set; } = string.Empty;

        [Parameter("uint8", "kind", 3, false)]
        public byte Kind { get; set; }

        [Parameter("bytes32", "subjectId", 4, false)]
        public byte[] SubjectId { get; set; } = Array.Empty<byte>();
    }

    [Event("AccountCreated")]
    public class AccountCreatedEvent : IEventDTO
    {
        [Parameter("address", "account", 1, true)]
        public string Account { get; set; } = string.Empty;

        [Parameter("address", "owner", 2, true)]
        public string Owner { get; set; } = string.Empty;

        [Parameter("uint256", "salt", 3, false)]
        public BigInteger Salt { get; set; }
    }

    [Event("PriceChanged")]
    public class PriceChangedEvent : IEventDTO
    {
        [Parameter("uint8", "tier", 1, true)]
        public byte Tier { get; set; }

        [Parameter("uint256", "price", 2, false)]
        public BigInteger Price { get; set; }
    }
}
