namespace MathChain.Domain.Interfaces
{
    public interface ICustodialSigner
    {
        string SignPersonalHash(Guid userId, byte[] hash);
    }
}
