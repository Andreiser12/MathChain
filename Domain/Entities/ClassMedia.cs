using System;

namespace MathChain.Domain.Entities
{
    public class ClassMedia
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ClassRoomId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string IpfsHash { get; set; } = string.Empty;
        public string UploaderWallet { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
