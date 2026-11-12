using MathChain.Application.DTOs;
using MathChain.Domain.Entities;

namespace MathChain.Application.Contexts
{
    public interface IUserContext
    {
        Task<LoginResult> LoginOrRegisterAsync(LoginRequest request);
        Task<User> GetByWalletAddressAsync(string walletAddress);
        Task<bool> CheckUserExistsAsync(string walletAddress);
        Task<List<User>> GetAllUsersAsync();
    }
}
