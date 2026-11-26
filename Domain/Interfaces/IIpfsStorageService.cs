namespace MathChain.Domain.Interfaces
{
    public interface IIpfsStorageService
    {
        public const int MaximumFileSize = 10 * 1024 * 1024;

        string ComputeHash(byte[] content);
        Task<string> SaveFileAsync(byte[] content);
        Task<byte[]?> GetFileAsync(string ipfsHash);
        Task<bool> FileExistsAsync(string ipfsHash);
        byte[] ToBinaryCid(string cid);
        string? FromBinaryCid(byte[] cid);
    }
}
