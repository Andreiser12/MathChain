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

            builder.HasIndex(c => c.ParentId);

            builder.Property(c => c.ChapterSlug)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(c => c.Title)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(c => c.Category)
                .HasMaxLength(100);

            builder.Property(c => c.Description)
                .HasMaxLength(1000);

            builder.Property(c => c.ImageUrl)
                .HasMaxLength(500);

            builder.Property(c => c.IpfsHash)
                .HasMaxLength(100);

            builder.Property(c => c.Order)
                .HasDefaultValue(0);

            builder.Property(c => c.FormulasCount)
                .HasDefaultValue(0);

            builder.Property(c => c.Version)
                .HasDefaultValue(1);

            builder.Property(c => c.UpdatedAt)
                .IsRequired();

            builder.HasOne(c => c.Parent)
                .WithMany(c => c.SubChapters)
                .HasForeignKey(c => c.ParentId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
