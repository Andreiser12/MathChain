namespace MathChain.Domain.Entities
{
    public class ChapterPrerequisite
    {
        public Guid ChapterId { get; set; }
        public Guid PrerequisiteId { get; set; }
    }
}
