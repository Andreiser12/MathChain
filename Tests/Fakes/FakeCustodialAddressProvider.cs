using MathChain.Domain.Interfaces;

namespace MathChain.Tests.Fakes
{
    public class FakeCustodialAddressProvider : ICustodialAddressProvider
    {
        public string GetOwnerAddress(Guid userId)
        {
            return $"0x{userId:N}00000000";
        }
    }
}
