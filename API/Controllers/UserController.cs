using MathChain.API.Data;
using MathChain.API.DTOs;
using MathChain.Domain.Entities;
using MathChain.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Asn1.Ocsp;

namespace MathChain.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly MathChainDbContext _context;

        public UserController(MathChainDbContext context)
        {
            _context = context;
        }

        private async Task<User?> FindUserAsync(string walletAddress)
        {
            if (string.IsNullOrWhiteSpace(walletAddress)) return null;
            var wallet = walletAddress.Trim().ToLower();
            var allUsers = await _context.Users.AsNoTracking().ToListAsync();
            return allUsers.FirstOrDefault(u => 
                !string.IsNullOrWhiteSpace(u.WalletAddress) && 
                (u.WalletAddress.Trim().Equals(wallet, StringComparison.OrdinalIgnoreCase) ||
                 u.WalletAddress.ToLower().Contains(wallet) ||
                 wallet.Contains(u.WalletAddress.ToLower())));
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.WalletAddress))
            {
                return BadRequest("WalletAddress is required.");
            }

            var wallet = request.WalletAddress.Trim().ToLower();
            var existingUser = await FindUserAsync(wallet);

            if (existingUser != null)
            {
                Console.WriteLine($"[Login] FOUND existing user: Id={existingUser.Id}, Wallet='{existingUser.WalletAddress}', Role={existingUser.Role}");
                return Ok(existingUser);
            }

            var isRoleProvided = !string.IsNullOrWhiteSpace(request.Role) 
                && !request.Role.Equals("null", StringComparison.OrdinalIgnoreCase) 
                && !request.Role.Equals("undefined", StringComparison.OrdinalIgnoreCase);

            if (!isRoleProvided)
            {
                return NotFound("User does not exist. A role is required for registration.");
            }

            UserRole parsedRole = UserRole.Teacher;
            if (!string.IsNullOrWhiteSpace(request.Role))
            {
                Enum.TryParse<UserRole>(request.Role, true, out parsedRole);
            }

            var newUser = new User
            {
                Id = Guid.NewGuid(),
                WalletAddress = wallet,
                Role = parsedRole,
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();
                Console.WriteLine($"[Login] CREATED new user: Id={newUser.Id}, Wallet='{newUser.WalletAddress}', Role={newUser.Role}");
                return Ok(newUser);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Login] Exception during save: {ex.Message}. Fetching fallback user...");
                _context.ChangeTracker.Clear();

                var fallbackUser = await FindUserAsync(wallet);
                if (fallbackUser != null)
                {
                    return Ok(fallbackUser);
                }

                return Ok(newUser);
            }
        }

        [HttpGet("exists/{walletAddress}")]
        public async Task<IActionResult> UserExists(string walletAddress)
        {
            var user = await FindUserAsync(walletAddress);
            return Ok(user != null);
        }
    }
}
