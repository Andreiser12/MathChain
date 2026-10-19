using MathChain.Domain.Entities;
using MathChain.Domain.Gateways;
using MathChain.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace MathChain.Gateway.Repositories
{
    public class GradeRepository : BaseRepository, IGradeGateway
    {
        public GradeRepository(MathChainDbContext context) : base(context)
        {

        }

        public async Task<Grade?> GetBySubmissionIdAsync(Guid submissionId)
        {
            return await _context.Grades
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.SubmissionId == submissionId);
        }

        public async Task<List<Grade>> GetBySubmissionIdsAsync(List<Guid> submissionIds)
        {
            if(submissionIds == null || !submissionIds.Any())
            {
                return new List<Grade>();
            }

            return await _context.Grades
                .AsNoTracking()
                .Where(g => submissionIds.Contains(g.SubmissionId))
                .ToListAsync();
        }

        public async Task<Grade> SaveAsync(Grade grade)
        {
            var existing = await _context.Grades
                .FirstOrDefaultAsync(g => g.SubmissionId == grade.SubmissionId);

            if(existing != null)
            {
                existing.Score = grade.Score;
                existing.Feedback = grade.Feedback;
                existing.GradedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return existing;
            }
            else
            {
                _context.Grades.Add(grade);
                await _context.SaveChangesAsync();
                return grade;
            }
        }
    }
}
