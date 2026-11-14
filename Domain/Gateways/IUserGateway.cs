using MathChain.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathChain.Domain.Gateways
{
    public interface IUserGateway
    {
        Task<User?> GetByWalletAddressAsync(string walletAddress);
        Task<User?> GetByIdAsync(Guid id);
        Task<List<User>> GetAllAsync();
        Task<User> CreateOrUpdateAsync(User user);
    }
}
