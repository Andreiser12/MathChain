using System.Text;
using System.Text.Json;
using MathChain.Blazor.DTOs;

namespace MathChain.Blazor.Services
{
    public partial class ApiService
    {
        public async Task<List<StudentGradebookDto>> GetGradebookAsync(Guid classRoomId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/classroom/{classRoomId}/gradebook");

                if(!response.IsSuccessStatusCode)
                {
                    return new List<StudentGradebookDto>();
                }

                var content = await response.Content.ReadAsStringAsync();

                return JsonSerializer.Deserialize<List<StudentGradebookDto>>(content, _jsonOptions) ?? new List<StudentGradebookDto>();
            }
            catch
            {
                return new List<StudentGradebookDto>();
            }
        }

        public async Task<bool> SetFinalGradeAsync(Guid classRoomId, string studentWallet, int finalGrade)
        {
            try
            {
                var json = JsonSerializer.Serialize(new
                {
                    FinalGrade = finalGrade
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/classroom/{classRoomId}/students/{studentWallet}/final-grade", content);

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> RemoveStudentFromClassAsync(Guid classRoomId, string studentWallet)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"{_baseUrl}/classroom/{classRoomId}/students/{studentWallet}");

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}
