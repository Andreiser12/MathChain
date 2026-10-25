using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathChain.Application.DTOs
{
    public class CreateClassMediaRequest
    {
        public Guid ClassRoomId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string IpfsHash { get; set; } = string.Empty;
        public string UploaderWallet { get; set; } = string.Empty;
    }

    public class CreateResourceRequest
    {
        public Guid ClassRoomId { get; set; }
        public int WeekNumber { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string IpfsHash { get; set; } = string.Empty;
    }
}
