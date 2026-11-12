namespace MathChain.Domain.Interfaces
{
    public interface IIpfsStorageService
    {
        string ComputeHash(byte[] content);
        Task<string> SaveFileAsync(byte[] content);
        Task<byte[]?> GetFileAsync(string ipfsHash);
        bool FileExists(string ipfsHash);
    }
}
