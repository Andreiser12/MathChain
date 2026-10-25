using MathChain.Application.Contexts;
using MathChain.Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace MathChain.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserContext _userContext;
        public UserController(IUserContext userContext)
        {
            _userContext = userContext;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var result = await _userContext.LoginOrRegisterAsync(request);

            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("exists/{walletAddress}")]
        public async Task<IActionResult> UserExists(string walletAddress)
        {
            var result = await _userContext.CheckUserExistsAsync(walletAddress);

            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("{walletAddress}")]
        public async Task<IActionResult> GetByWallet(string walletAddress)
        {
            var result = await _userContext.GetByWalletAddressAsync(walletAddress);

            return StatusCode(result.StatusCode, result);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            var result = await _userContext.GetAllUsersAsync();

            return StatusCode(result.StatusCode, result);
        }
    }
}
