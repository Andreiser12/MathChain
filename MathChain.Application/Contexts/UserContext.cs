using MathChain.Domain.Entities;
using MathChain.Domain.Enums;
using MathChain.Domain.Gateways;
using MathChain.Domain.Exceptions;

namespace MathChain.Application.Contexts
{
    public class UserContext : IUserContext
    {
        private readonly IUserGateway _userGateway;
        private readonly AccessGuard _access;

        public UserContext(IUserGateway userGateway, AccessGuard access)
        {
            _userGateway = userGateway;
            _access = access;
        }

        public async Task<User> GetByWalletAddressAsync(string walletAddress)
        {
            if (string.IsNullOrWhiteSpace(walletAddress))
            {
                throw new BusinessRuleException("Wallet address is required.");
            }

            var wallet = NormalizeWalletAddress(walletAddress);

            _access.RequireSelfOrAdmin(wallet);

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
            _access.RequireRole(UserRole.Admin);

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
