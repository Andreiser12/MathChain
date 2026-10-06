using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathChain.Domain.Entities
{
    public class MessageComment
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid MessageId { get; set; }
        public string AuthorWallet { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
