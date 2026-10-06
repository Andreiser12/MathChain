using System;

namespace MathChain.API.DTOs
{
    public class CreateClassMediaRequest
    {
        public Guid ClassRoomId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string IpfsHash { get; set; } = string.Empty;
        public string UploaderWallet { get; set; } = string.Empty;
    }
}
