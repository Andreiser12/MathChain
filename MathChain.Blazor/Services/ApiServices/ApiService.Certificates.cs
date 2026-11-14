using System.Net.Http.Json;
using System.Text.Json;
using MathChain.Blazor.DTOs;

namespace MathChain.Blazor.Services
{
    public partial class ApiService
    {
        private const string ServerUnreachableMessage = "The server could not be reached.";

        public async Task<ApiResult<List<ClassCertificateDto>>> IssueClassCertificatesAsync(Guid classRoomId)
        {
            return await SendForResultAsync<List<ClassCertificateDto>>(() =>
                _httpClient.PostAsJsonAsync($"{_baseUrl}/classroom/{classRoomId}/certificates", new { }));
        }

        public async Task<List<ClassCertificateDto>> GetClassCertificatesAsync(Guid classRoomId)
        {
            var result = await SendForResultAsync<List<ClassCertificateDto>>(() =>
                _httpClient.GetAsync($"{_baseUrl}/classroom/{classRoomId}/certificates"));

            return result.Data ?? new List<ClassCertificateDto>();
        }

        public async Task<ApiResult<object>> RevokeCertificateAsync(Guid certificateId)
        {
            return await SendForResultAsync<object>(() => _httpClient.DeleteAsync($"{_baseUrl}/certificates/{certificateId}"));
        }

        public async Task<List<MyCertificateDto>> GetMyCertificatesAsync()
        {
            var result = await SendForResultAsync<List<MyCertificateDto>>(() => _httpClient.GetAsync($"{_baseUrl}/certificates/mine"));

            return result.Data ?? new List<MyCertificateDto>();
        }

        public async Task<ApiResult<CertificateVerificationDto>> VerifyCertificateAsync(long tokenId, string? verificationKey)
        {
            var url = $"{_baseUrl}/certificates/verify/{tokenId}";

            if (!string.IsNullOrWhiteSpace(verificationKey))
            {
                url += $"?key={Uri.EscapeDataString(verificationKey.Trim())}";
            }

            return await SendForResultAsync<CertificateVerificationDto>(() => _httpClient.GetAsync(url));
        }

        public async Task<ApiResult<CertificateSetupDto>> GetCertificateSetupAsync()
        {
            return await SendForResultAsync<CertificateSetupDto>(() => _httpClient.GetAsync($"{_baseUrl}/certificates/setup"));
        }

        public async Task<decimal> GetWalletBalanceAsync(string walletAddress)
        {
            var result = await SendForResultAsync<decimal>(() =>
                _httpClient.GetAsync($"{_baseUrl}/certificates/balance/{Uri.EscapeDataString(walletAddress)}"));

            return result.Data;
        }

        private async Task<ApiResult<T>> SendForResultAsync<T>(Func<Task<HttpResponseMessage>> send)
        {
            try
            {
                var response = await send();
                var content = await response.Content.ReadAsStringAsync();
                Envelope<T>? envelope = null;

                try
                {
                    envelope = JsonSerializer.Deserialize<Envelope<T>>(content, _jsonOptions);
                }
                catch (JsonException)
                {
                }

                if (response.IsSuccessStatusCode && envelope != null && envelope.Success)
                {
                    return new ApiResult<T> { Data = envelope.Data };
                }

                var message = string.IsNullOrWhiteSpace(envelope?.Message)
                    ? $"The request failed with status {(int)response.StatusCode}."
                    : envelope.Message;

                return new ApiResult<T> { Error = message };
            }
            catch (HttpRequestException)
            {
                return new ApiResult<T> { Error = ServerUnreachableMessage };
            }
        }
    }
}
