using MathChain.Domain.Entities;
using MathChain.Domain.Exceptions;
using MathChain.Domain.Interfaces;

namespace MathChain.Tests.Shared.Fakes
{
    public class FakeGoogleTokenVerifier : IGoogleTokenVerifier
    {
        private readonly Dictionary<string, string> _subjectsByToken = new();

        public string? ClientId { get; set; } = "test-client-id.apps.googleusercontent.com";

        public string IssueToken(string subject)
        {
            var token = $"token-for-{subject}";
            _subjectsByToken[token] = subject;
            return token;
        }

        public Task<GoogleIdentity> VerifyAsync(string idToken)
        {
            if (!_subjectsByToken.TryGetValue(idToken, out var subject))
            {
                throw new UnauthorizedException("Invalid Google sign-in token.");
            }

            return Task.FromResult(new GoogleIdentity { Subject = subject });
        }
    }
}
