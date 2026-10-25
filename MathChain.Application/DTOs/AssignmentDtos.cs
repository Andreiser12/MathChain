using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathChain.Application.DTOs
{
    public class CreateAssignmentRequest
    {
        public Guid ClassRoomId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime? DueTime { get; set; }
        public string? FileName { get; set; }
        public string? IpfsHash { get; set; }
    }
    public class AssignmentResponseDto
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
    public class SubmitAssignmentRequest
    {
        public string StudentWallet { get; set; } = string.Empty;
        public string? FileName { get; set; }
        public string? IpfsHash { get; set; }
        public string? Content { get; set; }
    }
    public class SubmissionResponseDto
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
    public class GradeSubmissionRequest
    {
        public double Score { get; set; }
        public string? Feedback { get; set; }
    }
}
