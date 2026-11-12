using MathChain.Domain.Entities;
using MathChain.Domain.Gateways;

namespace MathChain.Tests.Fakes
{
    public class InMemorySubmissionGateway : ISubmissionGateway
    {
        public List<Submission> Submissions { get; } = new();

        public Task<List<Submission>> GetByAssignmentIdAsync(Guid assignmentId)
        {
            return Task.FromResult(Submissions.Where(s => s.AssignmentId == assignmentId).OrderBy(s => s.SubmittedAt).ToList());
        }

        public Task<Submission?> GetByIdAsync(Guid id)
        {
            return Task.FromResult(Submissions.FirstOrDefault(s => s.Id == id));
        }

        public Task<Submission?> GetByStudentAndAssignmentAsync(Guid assignmentId, string studentWallet)
        {
            return Task.FromResult(Submissions.FirstOrDefault(s => s.AssignmentId == assignmentId && s.StudentWallet == studentWallet));
        }

        public Task<Submission> SaveAsync(Submission submission)
        {
            Submissions.Add(submission);
            return Task.FromResult(submission);
        }

        public Task<List<Submission>> GetAllAsync()
        {
            return Task.FromResult(Submissions.ToList());
        }

        public Task<List<Submission>> GetByAssignmentIdsAsync(List<Guid> assignmentIds)
        {
            return Task.FromResult(Submissions.Where(s => assignmentIds.Contains(s.AssignmentId)).ToList());
        }

        public Task<List<Submission>> GetPracticeSubmissionsAsync(string studentWallet, string formulaId)
        {
            var wallet = studentWallet.Trim().ToLower();
            return Task.FromResult(Submissions
                .Where(s => s.StudentWallet.ToLower() == wallet && s.Content.Contains($"Formula {formulaId}"))
                .ToList());
        }

        public Task<int> CountByStudentWalletAsync(string studentWallet)
        {
            var wallet = studentWallet.Trim().ToLower();
            return Task.FromResult(Submissions.Count(s => s.StudentWallet.ToLower() == wallet));
        }
    }
}
