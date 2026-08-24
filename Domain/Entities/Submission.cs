using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathChain.Domain.Entities
{
    public class Submission
    {
        public Guid Id { get; set; }
        public Guid AssignmentId { get; set; }
        public string StudentWallet { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; }
        
    }
}
