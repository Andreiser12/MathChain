using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathChain.Domain.Entities
{
    public class ClassRoom
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string TeacherWallet { get; set; } = string.Empty;
        public string JoinCode { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
