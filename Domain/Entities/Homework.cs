using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathChain.Domain.Entities
{
    public class Homework : AssignmentBase
    {
        public DateTime? DueTime { get; set; }
    }
}
