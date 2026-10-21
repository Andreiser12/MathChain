using MathChain.Infrastructure.ResponseHandling;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MathChain.Infrastructure.Ipfs
{
    public class IpfsStorageService : IIpfsStorageService
    {
        private readonly string _storagePath;

        public IpfsStorageService(IConfiguration? configuration = null)
        {
            var configuredPath = configuration?["IpfsStoragePath"];

            if(string.IsNullOrWhiteSpace(configuredPath))
            {
                _storagePath = configuredPath;
            }
            else
            {
                string devPath = Path.Combine(Directory.GetCurrentDirectory(), "Storage", "ipfs");
                _storagePath = Directory.Exists(devPath) ?
                    devPath : Path.Combine(AppContext.BaseDirectory, "Storage", "ipfs");
            }

            if(!Directory.Exists(_storagePath))
            {
                Directory.CreateDirectory(_storagePath);
            }
        }

        public string ComputeHash(byte[] content)
        {
            return IpfsHashHelper.ComputeCid(content);
        }

        public bool FileExists(string ipfsHash)
        {
            if(string.IsNullOrWhiteSpace(ipfsHash))
            {
                return false;
            }

            return File.Exists(Path.Combine(_storagePath, ipfsHash));
        }

        public async Task<byte[]?> GetFileAsync(string ipfsHash)
        {
            if(string.IsNullOrWhiteSpace(ipfsHash))
            {
                return null;
            }

            string filePath = Path.Combine(_storagePath, ipfsHash);

            if(!File.Exists(filePath))
            {
                return null;
            }

            return await File.ReadAllBytesAsync(filePath);
        }

        public async Task<Envelope<string>> SaveFileAsync(byte[] content)
        {
            if (content == null || content.Length == 0)
            {
                return EnvelopeExtensions.Failure<string>("Conținutul fișierului este gol.", 400);
            }

            try
            {
                string ipfsHash = IpfsHashHelper.ComputeCid(content);
                string destinationPath = Path.Combine(_storagePath, ipfsHash);

                if (!File.Exists(destinationPath))
                {
                    await File.WriteAllBytesAsync(destinationPath, content);
                }

                return EnvelopeExtensions.Success(ipfsHash, "Fișierul a fost stocat pe nodul IPFS cu succes.", 200);
            }
            catch (Exception ex)
            {
                return EnvelopeExtensions.Failure<string>($"Eroare la scrierea în nodul IPFS: {ex.Message}", 500);
            }
        }
    }
}
