using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathChain.Application.DTOs
{
    public class LoginRequest
    {
        public string WalletAddress { get; set; } = string.Empty;
        public string? Role { get; set; }
    }
}
