using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MathChain.Blazor.DTOs;

namespace MathChain.Blazor.Services
{
    public partial class ApiService
    {
        public async Task<SignInProvidersDto> GetSignInProvidersAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/auth/providers");

                return await ReadEnvelopeDataAsync<SignInProvidersDto>(response) ?? new SignInProvidersDto();
            }
            catch
            {
                return new SignInProvidersDto();
            }
        }

        public async Task<GoogleSignInResult> SignInWithGoogleAsync(string idToken, string? role)
        {
            var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/auth/google", new { IdToken = idToken, Role = role });

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return new GoogleSignInResult { RegistrationRequired = true };
            }

            var content = await response.Content.ReadAsStringAsync();
            Envelope<AuthResultDto>? envelope = null;

            try
            {
                envelope = JsonSerializer.Deserialize<Envelope<AuthResultDto>>(content, _jsonOptions);
            }
            catch (JsonException)
            {
            }

            if (response.IsSuccessStatusCode && envelope != null && envelope.Success && envelope.Data != null)
            {
                return new GoogleSignInResult { Auth = envelope.Data };
            }

            return new GoogleSignInResult { Error = envelope?.Message };
        }
    }
}
