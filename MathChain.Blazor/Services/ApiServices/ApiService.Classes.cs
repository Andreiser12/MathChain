using System.Text;
using System.Text.Json;
using MathChain.Blazor.DTOs;

namespace MathChain.Blazor.Services
{
    public partial class ApiService
    {
        public async Task<ClassRoomDto?> CreateClassRoomAsync(string name, string teacherWallet)
        {
            try
            {
                var json = JsonSerializer.Serialize(new { Name = name, TeacherWallet = teacherWallet });
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_baseUrl}/classroom/create", content);
                return await ReadEnvelopeDataAsync<ClassRoomDto>(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API Error in CreateClassRoomAsync]: {ex.Message}");
                return null;
            }
        }

        public async Task<ClassRoomDto?> JoinClassRoomAsync(string joinCode, string studentWallet)
        {
            try
            {
                var json = JsonSerializer.Serialize(new { JoinCode = joinCode, StudentWallet = studentWallet });
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_baseUrl}/classroom/join", content);
                return await ReadEnvelopeDataAsync<ClassRoomDto>(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API Error in JoinClassRoomAsync]: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> LeaveClassRoomAsync(string classRoomId, string studentWallet)
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

        public async Task<List<ClassRoomDto>> GetClassRoomsByTeacherAsync(string teacherWallet)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/classroom/by-teacher/{teacherWallet}");
                return await ReadEnvelopeListAsync<ClassRoomDto>(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API Error in GetClassRoomsByTeacherAsync]: {ex.Message}");
                return new List<ClassRoomDto>();
            }
        }

        public async Task<List<ClassRoomDto>> GetClassRoomsByStudentAsync(string studentWallet)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/classroom/by-student/{studentWallet}");
                return await ReadEnvelopeListAsync<ClassRoomDto>(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API Error in GetClassRoomsByStudentAsync]: {ex.Message}");
                return new List<ClassRoomDto>();
            }
        }

        public async Task<ClassRoomDto?> GetClassRoomByIdAsync(Guid id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/classroom/{id}");
                return await ReadEnvelopeDataAsync<ClassRoomDto>(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API Error in GetClassRoomByIdAsync]: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> DeleteClassRoomAsync(Guid id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"{_baseUrl}/classroom/{id}");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> LeaveClassRoomAsync(Guid classRoomId, string studentWallet)
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
