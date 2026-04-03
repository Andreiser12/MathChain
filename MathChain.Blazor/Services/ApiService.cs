using System.Text;
using System.Text.Json;

namespace MathChain.Blazor.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        public ApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _baseUrl = "http://localhost:5065/api";
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
    }
}
