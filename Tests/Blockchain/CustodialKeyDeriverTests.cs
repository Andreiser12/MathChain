using MathChain.Blockchain.Custody;

namespace MathChain.Tests.Blockchain
{
    public class CustodialKeyDeriverTests
    {
        private const string Secret = "test-master-secret-with-at-least-32-bytes!!";

        private static CustodialKeyDeriver CreateDeriver(string secret = Secret)
        {
            return new CustodialKeyDeriver(new CustodySettings { MasterSecret = secret });
        }

        [Theory]
        [InlineData(Secret, "11111111-2222-3333-4444-555555555555", "0xc1dae98e6a141323b6fc845fb41f516bad87d356")]
        [InlineData(Secret, "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee", "0xa106d85c0d051d63db56782c4790dac5f3238a41")]
        [InlineData("another-master-secret-with-at-least-32-bytes", "11111111-2222-3333-4444-555555555555", "0x33849c584de76fde6a927c4f44881fccc82c9b7f")]
        public void GetOwnerAddress_ShouldMatchAnIndependentReferenceImplementation_WhenGivenTheSameSecretAndUser(string secret, string userId, string expectedAddress)
        {
            Assert.Equal(expectedAddress, CreateDeriver(secret).GetOwnerAddress(Guid.Parse(userId)));
        }

        [Fact]
        public void GetOwnerAddress_ShouldReturnTheSameAddressAcrossInstances_WhenTheUserIsTheSame()
        {
            var userId = Guid.NewGuid();

            Assert.Equal(CreateDeriver().GetOwnerAddress(userId), CreateDeriver().GetOwnerAddress(userId));
        }

        [Fact]
        public void GetOwnerAddress_ShouldReturnDistinctLowercaseAddresses_WhenThereAreManyUsers()
        {
            var deriver = CreateDeriver();

            var addresses = Enumerable.Range(0, 200).Select(_ => deriver.GetOwnerAddress(Guid.NewGuid())).ToList();

            Assert.Equal(200, addresses.Distinct().Count());
            Assert.All(addresses, address => Assert.Matches("^0x[0-9a-f]{40}$", address));
        }

        [Theory]
        [InlineData("")]
        [InlineData("too-short-secret")]
        public void GetOwnerAddress_ShouldThrow_WhenTheSecretIsTooShort(string secret)
        {
            var deriver = CreateDeriver(secret);

            Assert.False(deriver.IsConfigured);
            Assert.Throws<InvalidOperationException>(() => deriver.GetOwnerAddress(Guid.NewGuid()));
        }

        [Fact]
        public void GetOwnerAddress_ShouldThrow_WhenTheUserIdIsEmpty()
        {
            Assert.Throws<ArgumentException>(() => CreateDeriver().GetOwnerAddress(Guid.Empty));
        }
    }
}
