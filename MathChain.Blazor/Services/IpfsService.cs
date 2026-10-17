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
        private readonly string _fallbackUrl = "http://localhost:5065/api/ipfs/upload";

        public IpfsService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _baseApi = configuration["ApiBaseUrl"] ?? "https://localhost:7146/api";
            _primaryUrl = $"{_baseApi}/ipfs/upload";
        }

        public async Task<string?> UploadFileToIpfsAsync(IBrowserFile file)
        {
            try
            {
                using var stream = file.OpenReadStream(maxAllowedSize: 20 * 1024 * 1024);
                using var streamContent = new StreamContent(stream);
                streamContent.Headers.ContentType = new MediaTypeHeaderValue(
                    string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);

                using var formData = new MultipartFormDataContent();
                formData.Add(streamContent, "file", file.Name);

                HttpResponseMessage response;
                try
                {
                    response = await _httpClient.PostAsync(_primaryUrl, formData);
                }
                catch
                {
                    using var fallbackStream = file.OpenReadStream(maxAllowedSize: 20 * 1024 * 1024);
                    using var fallbackStreamContent = new StreamContent(fallbackStream);
                    fallbackStreamContent.Headers.ContentType = new MediaTypeHeaderValue(
                        string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);

                    using var fallbackFormData = new MultipartFormDataContent();
                    fallbackFormData.Add(fallbackStreamContent, "file", file.Name);

                    response = await _httpClient.PostAsync(_fallbackUrl, fallbackFormData);
                }

                if (!response.IsSuccessStatusCode)
                {
                    var err = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Upload failed ({response.StatusCode}): {err}");
                    return null;
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();
                using var document = JsonDocument.Parse(jsonResponse);

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

                HttpResponseMessage response;
                try
                {
                    response = await _httpClient.GetAsync(url);
                }
                catch
                {
                    url = $"http://localhost:5065/api/ipfs/{ipfsHashOrUrl}";
                    response = await _httpClient.GetAsync(url);
                }

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[IPFS] Fetch failed ({response.StatusCode}) from: {url}");
                    try
                    {
                        var staticResponse = await _httpClient.GetAsync("data/curriculum/algebra_calcul_prescurtat.json");
                        if (staticResponse.IsSuccessStatusCode)
                        {
                            var staticJson = await staticResponse.Content.ReadAsStringAsync();
                            return JsonSerializer.Deserialize<ChapterCurriculumDto>(staticJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
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
                    var staticResponse = await _httpClient.GetAsync("data/curriculum/algebra_calcul_prescurtat.json");
                    if (staticResponse.IsSuccessStatusCode)
                    {
                        var staticJson = await staticResponse.Content.ReadAsStringAsync();
                        return JsonSerializer.Deserialize<ChapterCurriculumDto>(staticJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    }
                }
                catch { }
                return null;
            }
        }
    }
}
