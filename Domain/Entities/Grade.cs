using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathChain.Domain.Entities
{
    public class Grade
    {
        public Guid Id { get; set; }
        public Guid SubmissionId { get; set; }
        public double Score { get; set; }
        public string Feedback { get; set; } = string.Empty;
        public DateTime GradedAt { get; set; }
    }
}
