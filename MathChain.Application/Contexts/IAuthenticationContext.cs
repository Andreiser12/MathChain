using MathChain.Application.DTOs;

namespace MathChain.Application.Contexts
{
    public interface IAuthenticationContext
    {
        Task<LoginChallengeDto> CreateChallengeAsync(LoginChallengeRequest request);
        Task<AuthResultDto> VerifyAsync(VerifyLoginRequest request);
    }
}
