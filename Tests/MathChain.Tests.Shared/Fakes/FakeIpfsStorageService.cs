using System.Text;
using MathChain.Domain.Interfaces;

namespace MathChain.Tests.Shared.Fakes
{
    public class FakeIpfsStorageService : IIpfsStorageService
    {
        public List<string> SavedFiles { get; } = new();
        public bool ShouldFail { get; set; }

        public string ComputeHash(byte[] content)
        {
            return $"QmFake{content.Length}";
        }

        public Task<string> SaveFileAsync(byte[] content)
        {
            if (ShouldFail)
            {
                throw new InvalidOperationException("IPFS node is unreachable.");
            }

            SavedFiles.Add(Encoding.UTF8.GetString(content));
            return Task.FromResult($"QmFake{SavedFiles.Count}");
        }

        public Task<byte[]?> GetFileAsync(string ipfsHash)
        {
            return Task.FromResult<byte[]?>(null);
        }

        public Task<bool> FileExistsAsync(string ipfsHash)
        {
            return Task.FromResult(false);
        }

        public byte[] ToBinaryCid(string cid)
        {
            return System.Text.Encoding.UTF8.GetBytes(cid);
        }

        public string? FromBinaryCid(byte[] cid)
        {
            return System.Text.Encoding.UTF8.GetString(cid);
        }
    }
}
