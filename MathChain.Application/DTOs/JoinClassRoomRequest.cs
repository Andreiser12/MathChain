using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathChain.Application.DTOs
{
    public class JoinClassRoomRequest
    {
        public string JoinCode { get; set; } = string.Empty;
        public string StudentWallet { get; set; } = string.Empty;
    }
}
