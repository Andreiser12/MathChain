namespace MathChain.Blazor.DTOs
{
    public class ClassMediaDto
    {
        public Guid Id { get; set; }
        public Guid ClassRoomId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string IpfsHash { get; set; } = string.Empty;
        public string UploaderWallet { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
