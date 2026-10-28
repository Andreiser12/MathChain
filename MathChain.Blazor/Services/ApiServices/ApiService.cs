using System.Collections.Concurrent;
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

        private static readonly ConcurrentDictionary<string, (DateTime CachedAt, ChapterDetailDto Data)> _chapterDetailCache = new();
        private static readonly ConcurrentDictionary<string, (DateTime CachedAt, List<CurriculumFormulaDto> Data)> _chapterFormulasCache = new();
        private static (DateTime CachedAt, List<CurriculumChapterDto> Data)? _rootChaptersCache;
        private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

        public static void InvalidateCurriculumCache()
        {
            _chapterDetailCache.Clear();
            _chapterFormulasCache.Clear();
            _rootChaptersCache = null;
        }

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

        public async Task<List<CurriculumChapterDto>> GetRootChaptersAsync(bool forceRefresh = false)
        {
            if (!forceRefresh && _rootChaptersCache.HasValue && _rootChaptersCache.Value.Data.Any() && (DateTime.UtcNow - _rootChaptersCache.Value.CachedAt) < CacheTtl)
            {
                return _rootChaptersCache.Value.Data;
            }

            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/curriculum/roots");
                var list = await ReadEnvelopeListAsync<CurriculumChapterDto>(response);
                if (list.Any())
                {
                    _rootChaptersCache = (DateTime.UtcNow, list);
                }
                return list;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiService] Error fetching root chapters: {ex.Message}");
                if (_rootChaptersCache.HasValue && _rootChaptersCache.Value.Data.Any())
                {
                    return _rootChaptersCache.Value.Data;
                }
                return new List<CurriculumChapterDto>();
            }
        }

        public async Task<ChapterDetailDto?> GetChapterDetailAsync(string idOrSlug, bool forceRefresh = false)
        {
            var key = idOrSlug.Trim().ToLowerInvariant();
            if (!forceRefresh && _chapterDetailCache.TryGetValue(key, out var cached) && (DateTime.UtcNow - cached.CachedAt) < CacheTtl)
            {
                return cached.Data;
            }

            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/curriculum/{Uri.EscapeDataString(idOrSlug)}");
                var data = await ReadEnvelopeDataAsync<ChapterDetailDto>(response);
                if (data != null && data.Chapter != null)
                {
                    _chapterDetailCache[key] = (DateTime.UtcNow, data);
                    _chapterDetailCache[data.Chapter.Id.ToString()] = (DateTime.UtcNow, data);
                    _chapterDetailCache[data.Chapter.ChapterSlug.ToLowerInvariant()] = (DateTime.UtcNow, data);
                }
                return data;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiService] Error fetching chapter detail '{idOrSlug}': {ex.Message}");
                if (_chapterDetailCache.TryGetValue(key, out var cachedFallback))
                {
                    return cachedFallback.Data;
                }
                return null;
            }
        }

        public async Task<CurriculumChapterDto?> CreateChapterAsync(CreateChapterDto dto)
        {
            InvalidateCurriculumCache();
            var json = JsonSerializer.Serialize(dto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_baseUrl}/curriculum", content);
            return await ReadEnvelopeDataAsync<CurriculumChapterDto>(response);
        }

        public async Task<CurriculumChapterDto?> UpdateChapterAsync(Guid id, UpdateChapterDto dto)
        {
            InvalidateCurriculumCache();
            var json = JsonSerializer.Serialize(dto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"{_baseUrl}/curriculum/{id}", content);
            return await ReadEnvelopeDataAsync<CurriculumChapterDto>(response);
        }

        public async Task<bool> DeleteChapterAsync(Guid id)
        {
            InvalidateCurriculumCache();
            var response = await _httpClient.DeleteAsync($"{_baseUrl}/curriculum/{id}");
            return await ReadEnvelopeSuccessAsync(response);
        }

        public async Task<List<CurriculumFormulaDto>> GetFormulasByChapterAsync(string idOrSlug, bool forceRefresh = false)
        {
            var key = idOrSlug.Trim().ToLowerInvariant();
            if (!forceRefresh && _chapterFormulasCache.TryGetValue(key, out var cached) && (DateTime.UtcNow - cached.CachedAt) < CacheTtl)
            {
                return cached.Data;
            }

            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/curriculum/{Uri.EscapeDataString(idOrSlug)}/formulas");
                var list = await ReadEnvelopeListAsync<CurriculumFormulaDto>(response);
                if (list.Any())
                {
                    _chapterFormulasCache[key] = (DateTime.UtcNow, list);
                }
                return list;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiService] Error fetching formulas for '{idOrSlug}': {ex.Message}");
                if (_chapterFormulasCache.TryGetValue(key, out var cachedFallback))
                {
                    return cachedFallback.Data;
                }
                return new List<CurriculumFormulaDto>();
            }
        }

        public async Task<CurriculumFormulaDto?> CreateFormulaAsync(Guid chapterId, CreateFormulaDto dto)
        {
            InvalidateCurriculumCache();
            var json = JsonSerializer.Serialize(dto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_baseUrl}/curriculum/{chapterId}/formulas", content);
            return await ReadEnvelopeDataAsync<CurriculumFormulaDto>(response);
        }

        public async Task<CurriculumFormulaDto?> UpdateFormulaAsync(Guid id, UpdateFormulaDto dto)
        {
            InvalidateCurriculumCache();
            var json = JsonSerializer.Serialize(dto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"{_baseUrl}/curriculum/formulas/{id}", content);
            return await ReadEnvelopeDataAsync<CurriculumFormulaDto>(response);
        }

        public async Task<bool> DeleteFormulaAsync(Guid id)
        {
            InvalidateCurriculumCache();
            var response = await _httpClient.DeleteAsync($"{_baseUrl}/curriculum/formulas/{id}");
            return await ReadEnvelopeSuccessAsync(response);
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
