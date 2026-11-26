using MathChain.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MathChain.Persistence.Configurations
{
    public class ChapterPrerequisiteConfiguration : IEntityTypeConfiguration<ChapterPrerequisite>
    {
        public void Configure(EntityTypeBuilder<ChapterPrerequisite> builder)
        {
            builder.ToTable("ChapterPrerequisites", table =>
                table.HasCheckConstraint("CK_ChapterPrerequisites_NotSelf", "\"ChapterId\" <> \"PrerequisiteId\""));

            builder.HasKey(p => new { p.ChapterId, p.PrerequisiteId });

            builder.HasIndex(p => p.PrerequisiteId);

            builder.HasOne<CurriculumChapter>()
                .WithMany()
                .HasForeignKey(p => p.ChapterId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne<CurriculumChapter>()
                .WithMany()
                .HasForeignKey(p => p.PrerequisiteId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
