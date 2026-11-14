namespace MathChain.Blazor.DTOs
{
    public class LoginChallengeDto
    {
        public string Message { get; set; } = string.Empty;
        public DateTimeOffset ExpiresAt { get; set; }
    }

    public class SignInProvidersDto
    {
        public string? GoogleClientId { get; set; }
    }

    public class GoogleSignInResult
    {
        public AuthResultDto? Auth { get; set; }
        public bool RegistrationRequired { get; set; }
        public string? Error { get; set; }
    }

    public class AuthResultDto
    {
        public string Token { get; set; } = string.Empty;
        public DateTimeOffset ExpiresAt { get; set; }
        public UserDto User { get; set; } = new();
        public bool IsNewUser { get; set; }
    }
}
