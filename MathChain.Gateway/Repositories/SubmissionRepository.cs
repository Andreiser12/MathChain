using MathChain.Domain.Entities;
using MathChain.Domain.Gateways;
using MathChain.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace MathChain.Gateway.Repositories
{
    public class SubmissionRepository : BaseRepository, ISubmissionGateway
    {
        public SubmissionRepository(MathChainDbContext context) : base(context)
        {

        }

        public async Task<List<Submission>> GetByAssignmentIdAsync(Guid assignmentId)
        {
            return await _context.Submissions
                .AsNoTracking()
                .Where(s => s.AssignmentId == assignmentId)
                .OrderBy(s => s.SubmittedAt)
                .ToListAsync();
        }

        public async Task<List<Submission>> GetAllAsync()
        {
            return await _context.Submissions
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<Submission>> GetByAssignmentIdsAsync(List<Guid> assignmentIds)
        {
            return await _context.Submissions
                .AsNoTracking()
                .Where(s => assignmentIds.Contains(s.AssignmentId))
                .ToListAsync();
        }

        public async Task<List<Submission>> GetPracticeSubmissionsAsync(string studentWallet, string formulaId)
        {
            var wallet = studentWallet.Trim().ToLower();
            var formulaMarker = $"Formula {formulaId}";

            return await _context.Submissions
                .AsNoTracking()
                .Where(s => s.StudentWallet.ToLower() == wallet && s.Content.Contains(formulaMarker))
                .ToListAsync();
        }

        public async Task<int> CountByStudentWalletAsync(string studentWallet)
        {
            var wallet = studentWallet.Trim().ToLower();

            return await _context.Submissions
                .CountAsync(s => s.StudentWallet.ToLower() == wallet);
        }

        public async Task<Submission?> GetByIdAsync(Guid id)
        {
            return await _context.Submissions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<Submission?> GetByStudentAndAssignmentAsync(Guid assignmentId, string studentWallet)
        {
            return await _context.Submissions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.AssignmentId == assignmentId && s.StudentWallet == studentWallet);
        }

        public async Task<Submission> SaveAsync(Submission submission)
        {
            var existing = await _context.Submissions
                .FirstOrDefaultAsync(s => s.AssignmentId == submission.AssignmentId && 
                s.StudentWallet == submission.StudentWallet);

            if(existing != null)
            {
                existing.FileName = submission.FileName;
                existing.IpfsHash = submission.IpfsHash;
                existing.Content = submission.Content;
                existing.SubmittedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return existing;
            }
            else
            {
                _context.Submissions.Add(submission);
                await _context.SaveChangesAsync();

                return submission;
            }
        }
    }
}
