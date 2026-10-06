using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathChain.Domain.Entities
{
    public class Assignment : AssignmentBase
    {
        public DateTime StartTime { get; set; } = DateTime.UtcNow;
        public DateTime? DueTime { get; set; }
        public string? FileName { get; set; }
        public string? IpfsHash { get; set; }
    }
}
