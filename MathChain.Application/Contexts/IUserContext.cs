using MathChain.Domain.Entities;

namespace MathChain.Application.Contexts
{
    public interface IUserContext
    {
        Task<User> GetByWalletAddressAsync(string walletAddress);
        Task<bool> CheckUserExistsAsync(string walletAddress);
        Task<List<User>> GetAllUsersAsync();
    }
}
