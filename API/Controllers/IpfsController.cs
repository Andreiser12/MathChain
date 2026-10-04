using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;

namespace MathChain.API.Controllers
{
    public class FileUploadRequest
    {
        public IFormFile File { get; set; } = null!;
    }

    [ApiController]
    [Route("api/[controller]")]
    public class IpfsController : ControllerBase
    {
        private readonly HttpClient _httpClient;

        public IpfsController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
        }

        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(50 * 1024 * 1024)] // până la 50 MB
        public async Task<IActionResult> Upload([FromForm] FileUploadRequest request)
        {
            var file = request?.File;
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            var pinataJwt = Environment.GetEnvironmentVariable("PINATA_JWT");
            if (string.IsNullOrWhiteSpace(pinataJwt))
            {
                return StatusCode(500, "PINATA_JWT is not configured in backend .env");
            }

            try
            {
                using var stream = file.OpenReadStream();
                using var streamContent = new StreamContent(stream);
                streamContent.Headers.ContentType = new MediaTypeHeaderValue(
                    string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);

                using var formData = new MultipartFormDataContent();
                formData.Add(streamContent, "file", file.FileName);

                var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.pinata.cloud/pinning/pinFileToIPFS")
                {
                    Content = formData
                };
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", pinataJwt);

                var response = await _httpClient.SendAsync(httpRequest);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode((int)response.StatusCode, $"Pinata error: {responseContent}");
                }

                using var document = JsonDocument.Parse(responseContent);
                if (document.RootElement.TryGetProperty("IpfsHash", out var hashElement))
                {
                    return Ok(new { ipfsHash = hashElement.GetString() });
                }

                return StatusCode(500, "Failed to retrieve IpfsHash from Pinata response.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal upload error: {ex.Message}");
            }
        }

        [HttpGet("download/{hash}")]
        public async Task<IActionResult> Download(string hash, [FromQuery] string? fileName)
        {
            if (string.IsNullOrWhiteSpace(hash))
            {
                return BadRequest("Hash is required.");
            }

            var safeFileName = string.IsNullOrWhiteSpace(fileName) ? $"{hash}.bin" : fileName;
            var pinataGatewayUrl = $"https://gateway.pinata.cloud/ipfs/{hash}";

            try
            {
                var response = await _httpClient.GetAsync(pinataGatewayUrl, HttpCompletionOption.ResponseHeadersRead);
                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode((int)response.StatusCode, "Failed to fetch file from Pinata IPFS gateway.");
                }

                var stream = await response.Content.ReadAsStreamAsync();
                var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";

                // File(...) setează automat Content-Disposition: attachment; filename="safeFileName"
                // Acest lucru forțează descărcarea imediată pe PC în folderul Downloads, fără tab extern
                return File(stream, contentType, safeFileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Download error: {ex.Message}");
            }
        }
    }
}
