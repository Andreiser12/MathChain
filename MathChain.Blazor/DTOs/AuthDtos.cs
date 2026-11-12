namespace MathChain.Blazor.DTOs
{
    public class LoginChallengeDto
    {
        public string Message { get; set; } = string.Empty;
        public DateTimeOffset ExpiresAt { get; set; }
    }

    public class AuthResultDto
    {
        public string Token { get; set; } = string.Empty;
        public DateTimeOffset ExpiresAt { get; set; }
        public UserDto User { get; set; } = new();
        public bool IsNewUser { get; set; }
    }
}
