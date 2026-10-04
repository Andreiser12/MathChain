using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Text.Json;

namespace MathChain.Blazor.Services
{
    public class IpfsService
    {
        private readonly HttpClient _httpClient;
        private readonly string _primaryUrl;
        private readonly string _fallbackUrl = "http://localhost:5065/api/ipfs/upload";

        public IpfsService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            var baseApi = configuration["ApiBaseUrl"] ?? "https://localhost:7146/api";
            _primaryUrl = $"{baseApi}/ipfs/upload";
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
                    // Fallback la HTTP (port 5065) în caz că serverul rulează pe profilul HTTP
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
    }
}
