using System.Text;
using System.Text.Json;
using MathChain.Blazor.DTOs;

namespace MathChain.Blazor.Services
{
    public partial class ApiService
    {
        public async Task<ClassRoomDto> CreateClassRoomAsync(string name, string teacherWallet)
        {
            var json = JsonSerializer.Serialize(new { Name = name, TeacherWallet = teacherWallet });
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_baseUrl}/classroom/create", content);
            response.EnsureSuccessStatusCode();
            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ClassRoomDto>(responseContent, _jsonOptions);
        }

        public async Task<ClassRoomDto> JoinClassRoomAsync(string joinCode, string teacherWallet)
        {
            var json = JsonSerializer.Serialize(new { JoinCode = joinCode, TeacherWallet = teacherWallet });
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_baseUrl}/classroom/join", content);
            response.EnsureSuccessStatusCode();
            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ClassRoomDto>(responseContent, _jsonOptions);
        }

        public async Task<List<ClassRoomDto>> GetClassRoomsByTeacherAsync(string teacherWallet)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/classroom/by-teacher/{teacherWallet}");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<ClassRoomDto>>(content, _jsonOptions);
        }

        public async Task<List<ClassRoomDto>> GetClassRoomsByStudentAsync(string studentWallet)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/classroom/by-student/{studentWallet}");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<ClassRoomDto>>(content, _jsonOptions);
        }

        public async Task<ClassRoomDto?> GetClassRoomByIdAsync(Guid id)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/classroom/{id}");
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ClassRoomDto>(content, _jsonOptions);
        }

        public async Task<bool> DeleteClassRoomAsync(Guid id)
        {
            var response = await _httpClient.DeleteAsync($"{_baseUrl}/classroom/{id}");
            return response.IsSuccessStatusCode;
        }
    }
}
