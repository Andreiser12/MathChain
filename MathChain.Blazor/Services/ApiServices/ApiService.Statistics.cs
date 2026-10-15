using System.Text.Json;
using MathChain.Blazor.DTOs;

namespace MathChain.Blazor.Services
{
    public partial class ApiService
    {
        public async Task<ClassStatisticsDto?> GetClassStatisticsAsync(Guid classRoomId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/classroom/{classRoomId}/statistics");

                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();

                return JsonSerializer.Deserialize<ClassStatisticsDto>(content, _jsonOptions);
            }
            catch
            {
                return null;
            }
        }
    }
}
