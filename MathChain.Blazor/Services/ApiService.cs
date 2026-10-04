using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MathChain.Domain.Enums;

using Microsoft.Extensions.Configuration;

namespace MathChain.Blazor.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        public ApiService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _baseUrl = configuration["ApiBaseUrl"] ?? "https://localhost:7146/api";
        }

        public string GetIpfsDownloadUrl(string ipfsHash, string fileName)
        {
            return $"{_baseUrl}/ipfs/download/{ipfsHash}?fileName={Uri.EscapeDataString(fileName)}";
        }

        public async Task<string> SubmitSolutionAsync(Guid problemId, string walletAddress, double solution)
        {
            var json = JsonSerializer.Serialize(new
            {
                ProblemId = problemId,
                Solution = solution,
                WalletAddress = walletAddress
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/integral/submit", content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<string>(responseContent);
        }

        public async Task<bool> VerifySolutionAsync(Guid problemId, double solution)
        {
            var json = JsonSerializer.Serialize(new { ProblemId = problemId, Solution = solution });

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/integral/verify", content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<bool>(responseContent);
        }

        public async Task<string> PayForSolutionAsync(Guid problemId, string walletAddress)
        {
            var json = JsonSerializer.Serialize(new
            {
                ProblemId = problemId,
                WalletAddress = walletAddress
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/integral/pay", content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<string>(responseContent);
        }

        public async Task<decimal> GetWalletBalanceAsync(string walletAddress)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/integral/balance/{walletAddress}");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<decimal>(content);
        }

        public async Task<bool> CheckUserExistsAsync(string walletAddress)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/user/exists/{walletAddress}");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<bool>(content);
        }

        public async Task<UserDto?> LoginAsync(string walletAddress, string? role = null)
        {
            var json = JsonSerializer.Serialize(new { WalletAddress = walletAddress, Role = role });
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_baseUrl}/user/login", content);

            if (string.IsNullOrEmpty(role) && !response.IsSuccessStatusCode)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<UserDto>(responseContent, _jsonOptions);
        }

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

        public async Task<List<ClassMaterialDto>> GetMaterialsByClassAsync(Guid classRoomId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/classroom/{classRoomId}/materials");
                if (!response.IsSuccessStatusCode) return new List<ClassMaterialDto>();
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<ClassMaterialDto>>(content, _jsonOptions) ?? new List<ClassMaterialDto>();
            }
            catch
            {
                return new List<ClassMaterialDto>();
            }
        }

        public async Task<ClassMaterialDto?> AddMaterialAsync(Guid classRoomId, int weekNumber, string title, string description, string fileName, string ipfsHash)
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
                return JsonSerializer.Deserialize<ClassMaterialDto>(resContent, _jsonOptions);
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> DeleteMaterialAsync(Guid materialId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"{_baseUrl}/classroom/materials/{materialId}");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }

    public class UserDto
    {
        public Guid Id { get; set; }
        public string WalletAddress { get; set; }
        public UserRole Role { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ClassRoomDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string TeacherWallet { get; set; }
        public string JoinCode { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class ClassMaterialDto
    {
        public Guid Id { get; set; }
        public Guid ClassRoomId { get; set; }
        public int WeekNumber { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string IpfsHash { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
