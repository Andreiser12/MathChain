using MathChain.Domain.Entities;
using MathChain.Domain.Gateways;

namespace MathChain.Tests.Fakes
{
    public class InMemoryGradeGateway : IGradeGateway
    {
        public List<Grade> Grades { get; } = new();

        public Task<Grade?> GetBySubmissionIdAsync(Guid submissionId)
        {
            return Task.FromResult(EntityCopy.Detached(Grades.FirstOrDefault(g => g.SubmissionId == submissionId)));
        }

        public Task<List<Grade>> GetBySubmissionIdsAsync(List<Guid> submissionIds)
        {
            return Task.FromResult(Grades.Where(g => submissionIds.Contains(g.SubmissionId)).ToList());
        }

        public Task<Grade> SaveAsync(Grade grade)
        {
            var existing = Grades.FirstOrDefault(g => g.SubmissionId == grade.SubmissionId);

            if (existing != null)
            {
                existing.Score = grade.Score;
                existing.Feedback = grade.Feedback;
                existing.GradedAt = DateTime.UtcNow;
                return Task.FromResult(existing);
            }

            Grades.Add(grade);
            return Task.FromResult(grade);
        }

        public Task<List<Grade>> GetAllAsync()
        {
            return Task.FromResult(Grades.ToList());
        }
    }
}
