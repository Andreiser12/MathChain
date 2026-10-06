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

        public async Task<List<AssignmentItemDto>> GetAssignmentAsync(Guid classRoomId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/classroom/{classRoomId}/assignment");

                if(!response.IsSuccessStatusCode)
                {
                    return new List<AssignmentItemDto>();
                }

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<AssignmentItemDto>>(content, _jsonOptions) ?? new List<AssignmentItemDto>();
            }
            catch
            {
                return new List<AssignmentItemDto>();
            }
        }

        public async Task<bool> CreateAssignmentAsync(Guid classRoomId, string title, string description,
            DateTime startTime, DateTime? dueTime, string? fileName, string? ipfsHash)
        {
            try
            {
                var json = JsonSerializer.Serialize(new
                {
                    ClassRoomId = classRoomId,
                    Title = title,
                    Description = description,
                    StartTime = startTime,
                    DueTime = dueTime,
                    FileName = fileName,
                    IpfsHash = ipfsHash
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_baseUrl}/classroom/{classRoomId}/assignment", content);

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteAssignmentAsync(Guid assignmentId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"{_baseUrl}/classroom/assignments/{assignmentId}");

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<SubmissionItemDto>> GetAssignmentSubmissionsAsync(Guid assignmentId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/classroom/assignment/{assignmentId}/submissions");

                if(!response.IsSuccessStatusCode)
                {
                    return new List<SubmissionItemDto>();
                }

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<SubmissionItemDto>>(content, _jsonOptions) ?? new List<SubmissionItemDto>();
            }
            catch
            {
                return new List<SubmissionItemDto>();
            }
        }

        public async Task<bool> GradeSubmissionAsync(Guid submissionId, double score, string feedback)
        {
            try
            {
                var json = JsonSerializer.Serialize(new
                {
                    Score = score,
                    Feedback = feedback
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/classroom/submissions/{submissionId}/grade", content);

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

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

    public class ClassResourceDto
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

    public class AssignmentItemDto
    {
        public Guid Id { get; set; }
        public Guid ClassRoomId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime? DueTime { get; set; }
        public string? FileName { get; set; }
        public string? IpfsHash { get; set; }
        public DateTime CreatedAt { get; set; }
        public int SubmissionCount { get; set; }
    }

    public class SubmissionItemDto
    {
        public Guid Id { get; set; }
        public Guid AssignmentId { get; set; }
        public string StudentWallet { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string? FileName { get; set; }
        public string? IpfsHash { get; set; }
        public DateTime SubmittedAt { get; set; }
        public double? Score { get; set; }
        public string? Feedback { get; set; }
    }

    public class StudentGradebookDto
    {
        public string StudentWallet { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public int? FinalGrade { get; set; }
        public List<StudentGradeEntryDto> Grades { get; set; } = new();
    }

    public class StudentGradeEntryDto
    {
        public DateTime GradedAt { get; set; }
        public double Score { get; set; }
    }

    public class AnnouncementResponseDto
    {
        public Guid Id { get; set; }
        public Guid ClassRoomId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string AuthorWallet { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<CommentResponseDto> Comments { get; set; } = new();
    }
    public class CommentResponseDto
    {
        public Guid Id { get; set; }
        public Guid MessageId { get; set; }
        public string AuthorWallet { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class ClassMediaDto
    {
        public Guid Id { get; set; }
        public Guid ClassRoomId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string IpfsHash { get; set; } = string.Empty;
        public string UploaderWallet { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
