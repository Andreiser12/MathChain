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
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<bool>(content);
        }

        public async Task<UserDto?> LoginAsync(string walletAddress, string? role = null)
        {
            var json = JsonSerializer.Serialize(new { WalletAddress = walletAddress, Role = role });
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_baseUrl}/user/login", content);

            if (string.IsNullOrEmpty(role) && !response.IsSuccessStatusCode)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<UserDto>(responseContent, _jsonOptions);
        }
    }
}
