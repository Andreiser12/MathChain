namespace MathChain.Domain.Interfaces
{
    public interface ICustodialAddressProvider
    {
        string GetOwnerAddress(Guid userId);
    }
}
