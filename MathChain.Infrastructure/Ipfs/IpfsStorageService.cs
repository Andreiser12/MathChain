using MathChain.Infrastructure.ResponseHandling;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace MathChain.Infrastructure.Ipfs
{
    public class IpfsStorageService : IIpfsStorageService
    {
        private readonly string _storagePath;

        public IpfsStorageService(IConfiguration? configuration = null)
        {
            _storagePath = ResolveStoragePath(configuration);

            if (!Directory.Exists(_storagePath))
            {
                Directory.CreateDirectory(_storagePath);
            }

            Console.WriteLine($"[IPFS] Local node storage path active at: {_storagePath}");
        }

        private static string ResolveStoragePath(IConfiguration? configuration)
        {
            var configuredPath = configuration?["IpfsStoragePath"];
            if (!string.IsNullOrWhiteSpace(configuredPath))
            {
                if (Directory.Exists(configuredPath))
                {
                    return configuredPath;
                }

                var fullConfigured = Path.GetFullPath(configuredPath);
                if (Directory.Exists(fullConfigured))
                {
                    return fullConfigured;
                }
            }

            // Check candidate paths relative to current directory and base directory
            var candidates = new List<string>
            {
                Path.Combine(Directory.GetCurrentDirectory(), "Storage", "ipfs"),
                Path.Combine(Directory.GetCurrentDirectory(), "API", "Storage", "ipfs"),
                Path.Combine(AppContext.BaseDirectory, "Storage", "ipfs")
            };

            foreach (var candidate in candidates)
            {
                if (Directory.Exists(candidate))
                {
                    return candidate;
                }
            }

            // Walk up directory tree from AppContext.BaseDirectory to find API/Storage/ipfs or Storage/ipfs
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && dir.Exists)
            {
                var candidate1 = Path.Combine(dir.FullName, "Storage", "ipfs");
                if (Directory.Exists(candidate1))
                {
                    return candidate1;
                }

                var candidate2 = Path.Combine(dir.FullName, "API", "Storage", "ipfs");
                if (Directory.Exists(candidate2))
                {
                    return candidate2;
                }

                dir = dir.Parent;
            }

            // Fallback: create in current directory
            var fallback = Path.Combine(Directory.GetCurrentDirectory(), "Storage", "ipfs");
            return fallback;
        }

        public string ComputeHash(byte[] content)
        {
            return IpfsHashHelper.ComputeCid(content);
        }

        public bool FileExists(string ipfsHash)
        {
            if (string.IsNullOrWhiteSpace(ipfsHash))
            {
                return false;
            }

            string filePath = Path.Combine(_storagePath, ipfsHash);
            if (File.Exists(filePath))
            {
                return true;
            }

            var cleanHash = Path.GetFileNameWithoutExtension(ipfsHash);
            return Directory.GetFiles(_storagePath, $"{cleanHash}*").Length > 0;
        }

        public async Task<byte[]?> GetFileAsync(string ipfsHash)
        {
            if (string.IsNullOrWhiteSpace(ipfsHash))
            {
                return null;
            }

            string filePath = Path.Combine(_storagePath, ipfsHash);

            // 1. Check local node storage (exact match)
            if (File.Exists(filePath))
            {
                return await File.ReadAllBytesAsync(filePath);
            }

            // 2. Check if file is stored with an extension (e.g. Qm...png)
            var cleanHash = Path.GetFileNameWithoutExtension(ipfsHash);
            var matchingFiles = Directory.GetFiles(_storagePath, $"{cleanHash}*");
            if (matchingFiles.Length > 0)
            {
                return await File.ReadAllBytesAsync(matchingFiles[0]);
            }

            // 3. Fallback: Query remote IPFS gateways (Pinata, ipfs.io, Cloudflare) for files uploaded to global IPFS
            Console.WriteLine($"[IPFS] File '{ipfsHash}' not found in local node. Querying remote IPFS gateways...");
            try
            {
                using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                var gatewayUrls = new[]
                {
                    $"https://gateway.pinata.cloud/ipfs/{cleanHash}",
                    $"https://ipfs.io/ipfs/{cleanHash}",
                    $"https://cloudflare-ipfs.com/ipfs/{cleanHash}"
                };

                foreach (var url in gatewayUrls)
                {
                    try
                    {
                        var response = await httpClient.GetAsync(url);
                        if (response.IsSuccessStatusCode)
                        {
                            var bytes = await response.Content.ReadAsByteArrayAsync();
                            if (bytes != null && bytes.Length > 0)
                            {
                                // Cache locally in node storage for future instant retrieval
                                await File.WriteAllBytesAsync(filePath, bytes);
                                Console.WriteLine($"[IPFS] Retrieved '{cleanHash}' from {url} and pinned to local storage.");
                                return bytes;
                            }
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[IPFS] Gateway query error: {ex.Message}");
            }

            Console.WriteLine($"[IPFS] File '{ipfsHash}' not found locally or on IPFS gateways.");
            return null;
        }

        public async Task<Envelope<string>> SaveFileAsync(byte[] content)
        {
            if (content == null || content.Length == 0)
            {
                return EnvelopeExtensions.Failure<string>("File content is empty.", 400);
            }

            try
            {
                string ipfsHash = IpfsHashHelper.ComputeCid(content);
                string destinationPath = Path.Combine(_storagePath, ipfsHash);

                if (!File.Exists(destinationPath))
                {
                    await File.WriteAllBytesAsync(destinationPath, content);
                }

                return EnvelopeExtensions.Success(ipfsHash, "File stored successfully on IPFS node.", 200);
            }
            catch (Exception ex)
            {
                return EnvelopeExtensions.Failure<string>($"Error writing to IPFS node: {ex.Message}", 500);
            }
        }
    }
}
