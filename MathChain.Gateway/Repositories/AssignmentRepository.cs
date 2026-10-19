using MathChain.Domain.Entities;
using MathChain.Domain.Gateways;
using MathChain.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace MathChain.Gateway.Repositories
{
    public class AssignmentRepository : BaseRepository, IAssignmentGateway
    {
        public AssignmentRepository(MathChainDbContext context) : base(context)
        {

        }

        public async Task<Assignment> CreateAsync(Assignment assignment)
        {
            _context.Assignments.Add(assignment);

            await _context.SaveChangesAsync();

            return assignment;
        }

        public async Task DeleteAsync(Guid id)
        {
            var assignment = await _context.Assignments
                .OfType<Assignment>()
                .FirstOrDefaultAsync(a => a.Id == id);

            if(assignment != null)
            {
                var submissions = await _context.Submissions
                    .Where(s => s.AssignmentId == id)
                    .ToListAsync();

                var submissionIds = submissions.Select(s => s.Id).ToList();

                var grades = await _context.Grades
                    .Where(g => submissionIds.Contains(g.SubmissionId))
                    .ToListAsync();

                _context.Grades.RemoveRange(grades);
                _context.Submissions.RemoveRange(submissions);
                _context.Assignments.Remove(assignment);

                await _context.SaveChangesAsync();
            }

        }

        public async Task<List<Assignment>> GetByClassRoomIdAsync(Guid classRoomId)
        {
            return await _context.Assignments
                .OfType<Assignment>()
                .AsNoTracking()
                .Where(a => a.ClassRoomId == classRoomId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<Assignment?> GetByIdAsync(Guid id)
        {
            return await _context.Assignments
                .OfType<Assignment>()
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == id);
        }
    }
}
