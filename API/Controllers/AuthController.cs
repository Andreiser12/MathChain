using MathChain.Application.Contexts;
using MathChain.Application.DTOs;
using MathChain.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MathChain.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ApiControllerBase
    {
        private readonly IAuthenticationContext _authenticationContext;

        public AuthController(IAuthenticationContext authenticationContext)
        {
            _authenticationContext = authenticationContext;
        }

        [HttpPost("challenge")]
        public async Task<IActionResult> CreateChallenge([FromBody] LoginChallengeRequest request)
        {
            var challenge = await _authenticationContext.CreateChallengeAsync(request);

            return Success(challenge, "Sign this message with your wallet to log in.");
        }

        [HttpPost("verify")]
        public async Task<IActionResult> Verify([FromBody] VerifyLoginRequest request)
        {
            var result = await _authenticationContext.VerifyAsync(request);

            if (result.IsNewUser)
            {
                return Success(result, "User registered successfully.", StatusCodes.Status201Created);
            }

            return Success(result, "User logged in successfully.");
        }

        [Authorize]
        [HttpGet("me")]
        public IActionResult Me()
        {
            var currentUser = new CurrentUserDto
            {
                WalletAddress = User.FindFirst(WalletClaimTypes.Wallet)?.Value ?? string.Empty,
                Role = User.FindFirst(WalletClaimTypes.Role)?.Value ?? string.Empty
            };

            return Success(currentUser);
        }
    }
}
