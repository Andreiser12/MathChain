using MathChain.Application.DTOs;
using MathChain.Domain.Entities;
using MathChain.Domain.Enums;
using MathChain.Domain.Gateways;
using MathChain.Domain.Exceptions;

namespace MathChain.Application.Contexts
{
    public class UserContext : IUserContext
    {
        private readonly IUserGateway _userGateway;
        public UserContext(IUserGateway userGateway)
        {
            _userGateway = userGateway;
        }

        public async Task<LoginResult> LoginOrRegisterAsync(LoginRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.WalletAddress))
            {
                throw new BusinessRuleException("Wallet address is required.");
            }

            var wallet = NormalizeWalletAddress(request.WalletAddress);

            var existingUser = await _userGateway.GetByWalletAddressAsync(wallet);

            if (existingUser != null)
            {
                return new LoginResult { User = existingUser, IsNewUser = false };
            }

            var isRoleProvided = !string.IsNullOrWhiteSpace(request.Role)
                && !request.Role.Equals("null", StringComparison.OrdinalIgnoreCase)
                && !request.Role.Equals("undefined", StringComparison.OrdinalIgnoreCase);

            if (!isRoleProvided)
            {
                throw new NotFoundException("User does not exist. A valid role is required for registration.");
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

            return new LoginResult { User = createdUser, IsNewUser = true };
        }

        public async Task<User> GetByWalletAddressAsync(string walletAddress)
        {
            if (string.IsNullOrWhiteSpace(walletAddress))
            {
                throw new BusinessRuleException("Wallet address is required.");
            }

            var wallet = NormalizeWalletAddress(walletAddress);

            var user = await _userGateway.GetByWalletAddressAsync(wallet);

            if (user == null)
            {
                throw new NotFoundException($"User with wallet '{wallet}' was not found.");
            }

            return user;
        }

        public async Task<bool> CheckUserExistsAsync(string walletAddress)
        {
            if (string.IsNullOrWhiteSpace(walletAddress))
            {
                return false;
            }

            var wallet = NormalizeWalletAddress(walletAddress);

            var user = await _userGateway.GetByWalletAddressAsync(wallet);

            return user != null;
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            var users = await _userGateway.GetAllAsync();

            return users;
        }

        private static string NormalizeWalletAddress(string walletAddress)
        {
            var wallet = walletAddress.Trim().ToLowerInvariant();
            if (wallet.StartsWith("00x"))
            {
                wallet = wallet.Substring(1);
            }
            return wallet;
        }
    }
}
