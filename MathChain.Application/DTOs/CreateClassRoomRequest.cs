using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathChain.Application.DTOs
{
    public class CreateClassRoomRequest
    {
        public string Name { get; set; } = string.Empty;
        public string TeacherWallet { get; set; } = string.Empty;
    }
}
