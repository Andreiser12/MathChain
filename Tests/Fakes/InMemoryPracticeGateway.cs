using MathChain.Domain.Entities;
using MathChain.Domain.Gateways;

namespace MathChain.Tests.Fakes
{
    public class InMemoryPracticeGateway : IPracticeGateway
    {
        private readonly InMemoryUserGateway _users;
        private readonly InMemorySubmissionGateway _submissions;
        private readonly InMemoryGradeGateway _grades;

        public InMemoryPracticeGateway(InMemoryUserGateway users, InMemorySubmissionGateway submissions, InMemoryGradeGateway grades)
        {
            _users = users;
            _submissions = submissions;
            _grades = grades;
        }

        public Task RecordAttemptAsync(User? newUser, Submission submission, Grade grade)
        {
            if (newUser != null)
            {
                _users.Users.Add(newUser);
            }

            _submissions.Submissions.Add(submission);
            _grades.Grades.Add(grade);

            return Task.CompletedTask;
        }
    }
}
