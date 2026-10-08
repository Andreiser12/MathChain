namespace MathChain.Blazor.DTOs
{
    public class ClassRoomDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string TeacherWallet { get; set; }
        public string JoinCode { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
