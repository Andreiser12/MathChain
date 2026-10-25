using MathChain.Application.DTOs;
using MathChain.Domain.Entities;
using MathChain.Domain.Enums;
using MathChain.Domain.Gateways;
using MathChain.Infrastructure.ResponseHandling;

namespace MathChain.Application.Contexts
{
    public class UserContext : IUserContext
    {
        private readonly IUserGateway _userGateway;
        public UserContext(IUserGateway userGateway)
        {
            _userGateway = userGateway;
        }

        public async Task<Envelope<User>> LoginOrRegisterAsync(LoginRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.WalletAddress))
            {
                return EnvelopeExtensions.Failure<User>("Wallet address is required.", 400);
            }

            var wallet = request.WalletAddress.Trim().ToLower();

            var existingUser = await _userGateway.GetByWalletAddressAsync(wallet);

            if (existingUser != null)
            {
                return EnvelopeExtensions.Success(existingUser, "User logged in successfully.");
            }

            var isRoleProvided = !string.IsNullOrWhiteSpace(request.Role)
                && !request.Role.Equals("null", StringComparison.OrdinalIgnoreCase)
                && !request.Role.Equals("undefined", StringComparison.OrdinalIgnoreCase);

            if (!isRoleProvided)
            {
                return EnvelopeExtensions.Failure<User>("User does not exist. A valid role is required for registration.", 404);
            }

            var parsedRole = UserRole.Teacher;

            if (!string.IsNullOrWhiteSpace(request.Role))
            {
                Enum.TryParse(request.Role, true, out parsedRole);
            }

            var newUser = new User
            {
                Id = Guid.NewGuid(),
                WalletAddress = wallet,
                Role = parsedRole,
                CreatedAt = DateTime.UtcNow
            };

            var createdUser = await _userGateway.CreateOrUpdateAsync(newUser);

            return EnvelopeExtensions.Success(createdUser, "User registered successfully.", 201);
        }
        public async Task<Envelope<User>> GetByWalletAddressAsync(string walletAddress)
        {
            if (string.IsNullOrWhiteSpace(walletAddress))
            {
                return EnvelopeExtensions.Failure<User>("Wallet address is required.", 400);
            }

            var wallet = walletAddress.Trim().ToLower();

            var user = await _userGateway.GetByWalletAddressAsync(wallet);

            if (user == null)
            {
                return EnvelopeExtensions.Failure<User>($"User with wallet '{wallet}' was not found.", 404);
            }

            return EnvelopeExtensions.Success(user);
        }
        public async Task<Envelope<bool>> CheckUserExistsAsync(string walletAddress)
        {
            if (string.IsNullOrWhiteSpace(walletAddress))
            {
                return EnvelopeExtensions.Success(false);
            }

            var wallet = walletAddress.Trim().ToLower();

            var user = await _userGateway.GetByWalletAddressAsync(wallet);

            return EnvelopeExtensions.Success(user != null);
        }
        public async Task<Envelope<List<User>>> GetAllUsersAsync()
        {
            var users = await _userGateway.GetAllAsync();

            return EnvelopeExtensions.Success(users);
        }
    }
}
