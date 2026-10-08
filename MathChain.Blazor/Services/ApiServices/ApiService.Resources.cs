using System.Text;
using System.Text.Json;
using MathChain.Blazor.DTOs;

namespace MathChain.Blazor.Services
{
    public partial class ApiService
    {
        public async Task<List<ClassResourceDto>> GetResourcesByClassAsync(Guid classRoomId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/classroom/{classRoomId}/resources");
                if (!response.IsSuccessStatusCode) return new List<ClassResourceDto>();
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<ClassResourceDto>>(content, _jsonOptions) ?? new List<ClassResourceDto>();
            }
            catch
            {
                return new List<ClassResourceDto>();
            }
        }

        public async Task<ClassResourceDto?> AddResourceAsync(Guid classRoomId, int weekNumber, string title, string description, string fileName, string ipfsHash)
        {
            try
            {
                var json = JsonSerializer.Serialize(new
                {
                    ClassRoomId = classRoomId,
                    WeekNumber = weekNumber,
                    Title = title,
                    Description = description,
                    FileName = fileName,
                    IpfsHash = ipfsHash
                });
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_baseUrl}/classroom/{classRoomId}/materials", content);
                if (!response.IsSuccessStatusCode) return null;
                var resContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ClassResourceDto>(resContent, _jsonOptions);
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> DeleteResourceAsync(Guid resourceId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"{_baseUrl}/classroom/materials/{resourceId}");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}
