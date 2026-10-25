using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MathChain.Domain.Enums;
using MathChain.Blazor.DTOs;

using Microsoft.Extensions.Configuration;

namespace MathChain.Blazor.Services
{
    public partial class ApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        public ApiService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _baseUrl = configuration["ApiBaseUrl"] ?? "https://localhost:7146/api";
        }

        public string GetIpfsDownloadUrl(string ipfsHash, string fileName)
        {
            return $"{_baseUrl}/ipfs/download/{ipfsHash}?fileName={Uri.EscapeDataString(fileName)}";
        }

        public async Task<string> SubmitSolutionAsync(Guid problemId, string walletAddress, double solution)
        {
            var json = JsonSerializer.Serialize(new
            {
                ProblemId = problemId,
                Solution = solution,
                WalletAddress = walletAddress
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/integral/submit", content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<string>(responseContent);
        }

        public async Task<bool> VerifySolutionAsync(Guid problemId, double solution)
        {
            var json = JsonSerializer.Serialize(new { ProblemId = problemId, Solution = solution });

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/integral/verify", content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<bool>(responseContent);
        }

        public async Task<string> PayForSolutionAsync(Guid problemId, string walletAddress)
        {
            var json = JsonSerializer.Serialize(new
            {
                ProblemId = problemId,
                WalletAddress = walletAddress
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/integral/pay", content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<string>(responseContent);
        }

        public async Task<decimal> GetWalletBalanceAsync(string walletAddress)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/integral/balance/{walletAddress}");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<decimal>(content);
        }

        public async Task<bool> CheckUserExistsAsync(string walletAddress)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/user/exists/{walletAddress}");

            return await ReadEnvelopeDataAsync<bool>(response);
        }

        public async Task<UserDto?> LoginAsync(string walletAddress, string? role = null)
        {
            var json = JsonSerializer.Serialize(new
            {
                WalletAddress = walletAddress,
                Role = role
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/user/login", content);

            if (string.IsNullOrEmpty(role) && !response.IsSuccessStatusCode)
            {
                return null;
            }

            return await ReadEnvelopeDataAsync<UserDto>(response);
        }

        private async Task<T?> ReadEnvelopeDataAsync<T>(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                return default;
            }

            var content = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(content))
            {
                return default;
            }

            var envelope = JsonSerializer.Deserialize<Envelope<T>>(content, _jsonOptions);

            return envelope != null && envelope.Success ? envelope.Data : default;
        }

        private async Task<List<T>> ReadEnvelopeListAsync<T>(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                return new List<T>();
            }
            var content = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(content))
            {
                return new List<T>();
            }
            var envelope = JsonSerializer.Deserialize<Envelope<List<T>>>(content, _jsonOptions);
            return envelope?.Data ?? new List<T>();
        }

        private async Task<bool> ReadEnvelopeSuccessAsync(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                return false;
            }
            var content = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(content))
            {
                return false;
            }
            var envelope = JsonSerializer.Deserialize<Envelope<object>>(content, _jsonOptions);
            return envelope?.Success ?? false;
        }
    }
}
