using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathChain.Domain.Entities
{
    public class Enrollment
    {
        public Guid Id { get; set; }
        public Guid ClassRoomId { get; set; }
        public string StudentWallet { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
