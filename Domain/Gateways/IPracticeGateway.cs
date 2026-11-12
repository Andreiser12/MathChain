using MathChain.Domain.Entities;

namespace MathChain.Domain.Gateways
{
    public interface IPracticeGateway
    {
        Task RecordAttemptAsync(User? newUser, Submission submission, Grade grade);
    }
}
