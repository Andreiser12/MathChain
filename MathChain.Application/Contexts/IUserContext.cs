using MathChain.Application.DTOs;
using MathChain.Domain.Entities;
using MathChain.Infrastructure.ResponseHandling;

namespace MathChain.Application.Contexts
{
    public interface IUserContext
    {
        Task<Envelope<User>> LoginOrRegisterAsync(LoginRequest request);
        Task<Envelope<User>> GetByWalletAddressAsync(string walletAddress);
        Task<Envelope<bool>> CheckUserExistsAsync(string walletAddress);
        Task<Envelope<List<User>>> GetAllUsersAsync();
    }
}
