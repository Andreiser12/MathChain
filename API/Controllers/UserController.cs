using MathChain.Application.Contexts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MathChain.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ApiControllerBase
    {
        private readonly IUserContext _userContext;
        public UserController(IUserContext userContext)
        {
            _userContext = userContext;
        }

        [AllowAnonymous]
        [HttpGet("exists/{walletAddress}")]
        public async Task<IActionResult> UserExists(string walletAddress)
        {
            var exists = await _userContext.CheckUserExistsAsync(walletAddress);

            return Success(exists);
        }

        [HttpGet("{walletAddress}")]
        public async Task<IActionResult> GetByWallet(string walletAddress)
        {
            var user = await _userContext.GetByWalletAddressAsync(walletAddress);

            return Success(user);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _userContext.GetAllUsersAsync();

            return Success(users);
        }
    }
}
