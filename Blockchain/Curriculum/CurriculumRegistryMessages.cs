using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;

namespace MathChain.Blockchain.Curriculum
{
    [Function("publish", "uint256")]
    public class PublishCurriculumFunction : FunctionMessage
    {
        [Parameter("bytes", "cid", 1)]
        public byte[] Cid { get; set; } = Array.Empty<byte>();
    }

    [Function("publicationCount", "uint256")]
    public class PublicationCountFunction : FunctionMessage
    {
    }

    [Function("publicationAt", typeof(PublicationOutput))]
    public class PublicationAtFunction : FunctionMessage
    {
        [Parameter("uint256", "version", 1)]
        public BigInteger Version { get; set; }
    }

    public class PublicationStruct
    {
        [Parameter("bytes", "cid", 1)]
        public byte[] Cid { get; set; } = Array.Empty<byte>();

        [Parameter("uint64", "publishedAt", 2)]
        public ulong PublishedAt { get; set; }

        [Parameter("address", "publisher", 3)]
        public string Publisher { get; set; } = string.Empty;
    }

    [FunctionOutput]
    public class PublicationOutput : IFunctionOutputDTO
    {
        [Parameter("tuple", "", 1)]
        public PublicationStruct Publication { get; set; } = new();
    }

    [Event("CurriculumPublished")]
    public class CurriculumPublishedEvent : IEventDTO
    {
        [Parameter("uint256", "version", 1, true)]
        public BigInteger Version { get; set; }

        [Parameter("bytes", "cid", 2, false)]
        public byte[] Cid { get; set; } = Array.Empty<byte>();

        [Parameter("address", "publisher", 3, true)]
        public string Publisher { get; set; } = string.Empty;

        [Parameter("uint64", "publishedAt", 4, false)]
        public ulong PublishedAt { get; set; }
    }
}
