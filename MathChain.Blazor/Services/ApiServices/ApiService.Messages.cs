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

                return await ReadEnvelopeListAsync<AnnouncementResponseDto>(response);
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
                var json = JsonSerializer.Serialize(new
                { 
                    ClassRoomId = classRoomId,
                    Title = title,
                    Content = content,
                    AuthorWallet = authorWallet
                });

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
                var json = JsonSerializer.Serialize(new
                {
                    MessageId = messageId,
                    Content = content,
                    AuthorWallet = authorWallet
                });

                var body = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/classroom/messages/{messageId}/comments", body);

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteMessageCommentAsync(Guid commentId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"{_baseUrl}/classroom/messages/comments/{commentId}");

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}
