using System.Text;
using System.Text.Json;
using MathChain.Blazor.DTOs;

namespace MathChain.Blazor.Services
{
    public partial class ApiService
    {
        public async Task<List<AssignmentItemDto>> GetAssignmentAsync(Guid classRoomId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/classroom/{classRoomId}/assignment");

                return await ReadEnvelopeListAsync<AssignmentItemDto>(response);
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

                return await ReadEnvelopeListAsync<SubmissionItemDto>(response);
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

        public async Task<bool> SubmitAssignmentAsync(Guid assignmentId, string studentWallet, string? fileName, string? ipfsHash, string? content)
        {
            try
            {
                var json = JsonSerializer.Serialize(new
                {
                    StudentWallet = studentWallet,
                    FileName = fileName,
                    IpfsHash = ipfsHash,
                    Content = content ?? string.Empty
                });

                var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/classroom/assignment/{assignmentId}/submit", httpContent);

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}
