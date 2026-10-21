using MathChain.Infrastructure.ResponseHandling;

namespace MathChain.Infrastructure.Ipfs
{
    public interface IIpfsStorageService
    {
        string ComputeHash(byte[] content);
        Task<Envelope<string>> SaveFileAsync(byte[] content);
        Task<byte[]?> GetFileAsync(string ipfsHash);
        bool FileExists(string ipfsHash);
    }
}
