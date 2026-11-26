using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Text.Json;
using MathChain.Blazor.DTOs;

namespace MathChain.Blazor.Services
{
    public class IpfsService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseApi;
        private readonly string _primaryUrl;

        public const long MaximumFileSize = 10 * 1024 * 1024;

        public IpfsService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _baseApi = configuration["ApiBaseUrl"] ?? "https://localhost:7146/api";
            _primaryUrl = $"{_baseApi}/ipfs/upload";
        }

        public async Task<string?> UploadFileToIpfsAsync(IBrowserFile file)
        {
            if (file.Size > MaximumFileSize)
            {
                Console.WriteLine($"Upload rejected: {file.Name} is larger than 10 MB.");
                return null;
            }

            try
            {
                using var stream = file.OpenReadStream(maxAllowedSize: MaximumFileSize);
                using var streamContent = new StreamContent(stream);

                streamContent.Headers.ContentType = new MediaTypeHeaderValue(
                    string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);

                using var formData = new MultipartFormDataContent();
                formData.Add(streamContent, "file", file.Name);

                var response = await _httpClient.PostAsync(_primaryUrl, formData);

                if (!response.IsSuccessStatusCode)
                {
                    var err = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Upload failed ({response.StatusCode}): {err}");

                    return null;
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();
                using var document = JsonDocument.Parse(jsonResponse);

                if (document.RootElement.TryGetProperty("data", out var dataElement) && dataElement.ValueKind == JsonValueKind.String)
                {
                    return dataElement.GetString();
                }
                if (document.RootElement.TryGetProperty("ipfsHash", out var hashElement))
                {
                    return hashElement.GetString();
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading file to IPFS via Backend: {ex.Message}");
                return null;
            }
        }

        private Dictionary<string, string>? _manifestCache;

        private async Task<string?> GetFallbackPathAsync(string ipfsHash)
        {
            try
            {
                if (_manifestCache == null)
                {
                    var manifestRes = await _httpClient.GetAsync("data/curriculum/manifest.json");

                    if (manifestRes.IsSuccessStatusCode)
                    {
                        var manifestJson = await manifestRes.Content.ReadAsStringAsync();
                        _manifestCache = JsonSerializer.Deserialize<Dictionary<string, string>>(manifestJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    }
                }

                if (_manifestCache != null && _manifestCache.TryGetValue(ipfsHash, out var fileName))
                {
                    return $"data/curriculum/{fileName}";
                }
            }
            catch { }

            return "data/curriculum/algebra_calcul_prescurtat.json";
        }

        public async Task<ChapterCurriculumDto?> GetCurriculumFromIpfsAsync(string ipfsHashOrUrl)
        {
            try
            {
                string url;
                if (ipfsHashOrUrl.StartsWith("http") || ipfsHashOrUrl.StartsWith("/"))
                {
                    url = ipfsHashOrUrl;
                }
                else
                {
                    url = $"{_baseApi}/ipfs/{ipfsHashOrUrl}";
                }

                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[IPFS] Fetch failed ({response.StatusCode}) from: {url}");
                    try
                    {
                        var fallbackFile = await GetFallbackPathAsync(ipfsHashOrUrl);

                        if (!string.IsNullOrEmpty(fallbackFile))
                        {
                            var staticResponse = await _httpClient.GetAsync(fallbackFile);

                            if (staticResponse.IsSuccessStatusCode)
                            {
                                var staticJson = await staticResponse.Content.ReadAsStringAsync();

                                return JsonSerializer.Deserialize<ChapterCurriculumDto>(staticJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                            }
                        }
                    }
                    catch { }

                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                return JsonSerializer.Deserialize<ChapterCurriculumDto>(json, options);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[IPFS] Error fetching curriculum from IPFS: {ex.Message}");
                try
                {
                    var fallbackFile = await GetFallbackPathAsync(ipfsHashOrUrl);

                    if (!string.IsNullOrEmpty(fallbackFile))
                    {
                        var staticResponse = await _httpClient.GetAsync(fallbackFile);

                        if (staticResponse.IsSuccessStatusCode)
                        {
                            var staticJson = await staticResponse.Content.ReadAsStringAsync();

                            return JsonSerializer.Deserialize<ChapterCurriculumDto>(staticJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        }
                    }
                }
                catch { }

                return null;
            }
        }

        public async Task<ChapterCurriculumDto?> GetCurriculumBySlugAsync(string slug, string fallbackCid = "")
        {
            try
            {
                string url = $"{_baseApi}/curriculum/{slug}";
                var response = await _httpClient.GetAsync(url);

                if(response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();

                    using var doc = JsonDocument.Parse(json);

                    if(doc.RootElement.TryGetProperty("ipfsHash", out var hashEl))
                    {
                        var dynamicHash = hashEl.GetString();

                        if(!string.IsNullOrEmpty(dynamicHash))
                        {
                            var curriculumfromDbHash = await GetCurriculumFromIpfsAsync(dynamicHash);

                            if(curriculumfromDbHash != null)
                            {
                                return curriculumfromDbHash;
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"[Curriculum] Capitolul '{slug}' nu a fost găsit" +
                        $" în DB ({response.StatusCode}). Se folosește fallback.");
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine($"[Curriculum] Eroare la interogarea DB pentru '{slug}': {ex.Message}");
            }

            if(!string.IsNullOrEmpty(fallbackCid))
            {
                return await GetCurriculumFromIpfsAsync(fallbackCid);
            }

            return null;
        }
    }
}
