using System.Text;
using System.Text.Json;
using MathChain.Blazor.DTOs;

namespace MathChain.Blazor.Services
{
    public partial class ApiService
    {
        public async Task<List<AnnouncementResponseDto>> GetClassMessagesAsync(Guid classRoomId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/classroom/{classRoomId}/messages");

                if (!response.IsSuccessStatusCode)
                {
                    return new List<AnnouncementResponseDto>();
                }
                var content = await response.Content.ReadAsStringAsync();

                return JsonSerializer.Deserialize<List<AnnouncementResponseDto>>(content, _jsonOptions) ?? new List<AnnouncementResponseDto>();
            }
            catch
            {
                return new List<AnnouncementResponseDto>();
            }
        }
        public async Task<bool> CreateClassMessageAsync(Guid classRoomId, string title, string content, string authorWallet)
        {
            try
            {
                var json = JsonSerializer.Serialize(new { ClassRoomId = classRoomId, Title = title, Content = content, AuthorWallet = authorWallet });

                var body = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/classroom/{classRoomId}/messages", body);

                return response.IsSuccessStatusCode;
            }
            catch
            { 
                return false;
            }
        }
        public async Task<bool> DeleteClassMessageAsync(Guid messageId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"{_baseUrl}/classroom/messages/{messageId}");

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
        public async Task<bool> AddMessageCommentAsync(Guid messageId, string content, string authorWallet)
        {
            try
            {
                var json = JsonSerializer.Serialize(new { MessageId = messageId, Content = content, AuthorWallet = authorWallet });

                var body = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/classroom/messages/{messageId}/comments", body);

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}
