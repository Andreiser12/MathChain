using MathChain.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MathChain.Persistence.Configurations
{
    public class CurriculumChapterConfiguration : IEntityTypeConfiguration<CurriculumChapter>
    {
        public void Configure(EntityTypeBuilder<CurriculumChapter> builder)
        {
            builder.ToTable("CurriculumChapters");

            builder.HasKey(c => c.Id);

            builder.HasIndex(c => c.ChapterSlug)
                .IsUnique();

            builder.Property(c => c.ChapterSlug)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(c => c.Title)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(c => c.Category)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(c => c.IpfsHash)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(c => c.Version)
                .HasDefaultValue(1);

            builder.Property(c => c.UpdatedAt)
                .IsRequired();
        }
    }
}
