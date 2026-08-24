using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathChain.Domain.Entities
{
    public class Test : AssignmentBase
    {
        public DateTime OpensAt { get; set; }
        public DateTime ClosesAt { get; set; }
        public TimeSpan TimeLimit { get; set; }
    }
}
