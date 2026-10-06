using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathChain.Domain.Entities
{
    public class ClassAnnouncement
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ClassRoomId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string AuthorWallet { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
