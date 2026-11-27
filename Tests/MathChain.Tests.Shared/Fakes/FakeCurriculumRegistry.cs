using MathChain.Domain.Entities;
using MathChain.Domain.Exceptions;
using MathChain.Domain.Interfaces;

namespace MathChain.Tests.Shared.Fakes
{
    public class FakeCurriculumRegistry : ICurriculumRegistry
    {
        public const string ContractAddress = "0xc0ffee0000000000000000000000000000000001";
        public const string PublisherAddress = "0x70997970c51812dc3a010c7d01b50e0d17dc79c8";

        public List<CurriculumAnchor> Publications { get; } = new();
        public bool CanRead { get; set; } = true;
        public bool CanPublish { get; set; } = true;
        public bool Unreachable { get; set; }

        public CurriculumRegistryInfo Info => new()
        {
            NetworkName = "Test Network",
            ChainId = 31337,
            ExplorerUrl = "https://explorer.example",
            ContractAddress = ContractAddress,
            PublisherAddress = CanPublish ? PublisherAddress : null
        };

        public Task<CurriculumAnchor?> GetLatestAsync()
        {
            if (Unreachable)
            {
                throw new ServiceUnavailableException("The blockchain network could not be reached. Try again later.");
            }

            return Task.FromResult(Publications.LastOrDefault());
        }

        public Task<CurriculumAnchor> PublishAsync(byte[] cid)
        {
            if (Publications.Count > 0 && Publications[^1].Cid.SequenceEqual(cid))
            {
                throw new BusinessRuleException("This curriculum is already the latest published version.");
            }

            var anchor = new CurriculumAnchor
            {
                Version = Publications.Count + 1,
                Cid = cid,
                PublishedAt = new DateTime(2026, 11, 26, 12, 0, 0, DateTimeKind.Utc),
                Publisher = PublisherAddress,
                TransactionHash = "0x" + Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N")
            };
            Publications.Add(anchor);

            return Task.FromResult(anchor);
        }
    }
}
