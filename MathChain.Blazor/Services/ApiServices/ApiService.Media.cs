using System.Text;
using System.Text.Json;
using MathChain.Blazor.DTOs;

namespace MathChain.Blazor.Services
{
    public partial class ApiService
    {
        public async Task<List<ClassMediaDto>> GetClassMediaAsync(Guid classRoomId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/classroom/{classRoomId}/media");
                if (!response.IsSuccessStatusCode)
                {
                    return new List<ClassMediaDto>();
                }
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<ClassMediaDto>>(content, _jsonOptions) ?? new List<ClassMediaDto>();
            }
            catch
            {
                return new List<ClassMediaDto>();
            }
        }

        public async Task<bool> AddClassMediaAsync(Guid classRoomId, string fileName, string ipfsHash, string uploaderWallet)
        {
            try
            {
                var json = JsonSerializer.Serialize(new
                {
                    ClassRoomId = classRoomId,
                    FileName = fileName,
                    IpfsHash = ipfsHash,
                    UploaderWallet = uploaderWallet
                });
                var body = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_baseUrl}/classroom/{classRoomId}/media", body);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteClassMediaAsync(Guid mediaId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"{_baseUrl}/classroom/media/{mediaId}");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}
