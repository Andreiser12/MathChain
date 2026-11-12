using MathChain.Domain.Entities;
using MathChain.Domain.Gateways;
using MathChain.Persistence.Context;

namespace MathChain.Gateway.Repositories
{
    public class PracticeRepository : BaseRepository, IPracticeGateway
    {
        public PracticeRepository(MathChainDbContext context) : base(context)
        {
        }

        public async Task RecordAttemptAsync(User? newUser, Submission submission, Grade grade)
        {
            if (newUser != null)
            {
                _context.Users.Add(newUser);
            }

            _context.Submissions.Add(submission);
            _context.Grades.Add(grade);

            await _context.SaveChangesAsync();
        }
    }
}
