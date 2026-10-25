using System.Text;
using System.Text.Json;
using MathChain.Blazor.DTOs;

namespace MathChain.Blazor.Services
{
    public partial class ApiService
    {
        public async Task<ClassRoomDto?> CreateClassRoomAsync(string name, string teacherWallet)
        {
            var json = JsonSerializer.Serialize(new
            {
                Name = name,
                TeacherWallet = teacherWallet
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/classroom/create", content);

            return await ReadEnvelopeDataAsync<ClassRoomDto>(response);
        }

        public async Task<ClassRoomDto?> JoinClassRoomAsync(string joinCode, string studentWallet)
        {
            var json = JsonSerializer.Serialize(new
            {
                JoinCode = joinCode,
                StudentWallet = studentWallet
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/classroom/join", content);

            return await ReadEnvelopeDataAsync<ClassRoomDto>(response);
        }

        public async Task<bool> LeaveClassRoomAsync(string classRoomId, string studentWallet)
        {
            var response = await _httpClient.DeleteAsync($"{_baseUrl}/classroom/{classRoomId}/students/{studentWallet}");

            return response.IsSuccessStatusCode;
        }

        public async Task<List<ClassRoomDto>> GetClassRoomsByTeacherAsync(string teacherWallet)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/classroom/by-teacher/{teacherWallet}");

            return await ReadEnvelopeListAsync<ClassRoomDto>(response);
        }

        public async Task<List<ClassRoomDto>> GetClassRoomsByStudentAsync(string studentWallet)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/classroom/by-student/{studentWallet}");

            return await ReadEnvelopeListAsync<ClassRoomDto>(response);
        }

        public async Task<ClassRoomDto?> GetClassRoomByIdAsync(Guid id)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/classroom/{id}");

            return await ReadEnvelopeDataAsync<ClassRoomDto>(response);
        }

        public async Task<bool> DeleteClassRoomAsync(Guid id)
        {
            var response = await _httpClient.DeleteAsync($"{_baseUrl}/classroom/{id}");

            return response.IsSuccessStatusCode;
        }

        public async Task<bool> LeaveClassRoomAsync(Guid classRoomId, string studentWallet)
        {
            var response = await _httpClient.DeleteAsync($"{_baseUrl}/classroom/{classRoomId}/students/{studentWallet}");

            return response.IsSuccessStatusCode;
        }
    }
}
