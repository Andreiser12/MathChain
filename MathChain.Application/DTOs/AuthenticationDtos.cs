using MathChain.Domain.Entities;

namespace MathChain.Application.DTOs
{
    public class LoginChallengeRequest
    {
        public string WalletAddress { get; set; } = string.Empty;
    }

    public class LoginChallengeDto
    {
        public string Message { get; set; } = string.Empty;
        public DateTimeOffset ExpiresAt { get; set; }
    }

    public class VerifyLoginRequest
    {
        public string WalletAddress { get; set; } = string.Empty;
        public string Signature { get; set; } = string.Empty;
        public string? Role { get; set; }
    }

    public class AuthResultDto
    {
        public string Token { get; set; } = string.Empty;
        public DateTimeOffset ExpiresAt { get; set; }
        public User User { get; set; } = null!;
        public bool IsNewUser { get; set; }
    }

    public class CurrentUserDto
    {
        public string WalletAddress { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    public class SignInSettings
    {
        public string Domain { get; set; } = "localhost:7087";
        public string Uri { get; set; } = "https://localhost:7087";
        public long ChainId { get; set; } = 11155111;
        public int ChallengeLifetimeMinutes { get; set; } = 5;
    }
}
