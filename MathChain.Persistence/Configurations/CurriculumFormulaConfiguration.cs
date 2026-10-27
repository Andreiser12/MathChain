using MathChain.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MathChain.Persistence.Configurations
{
    public class CurriculumFormulaConfiguration : IEntityTypeConfiguration<CurriculumFormula>
    {
        public void Configure(EntityTypeBuilder<CurriculumFormula> builder)
        {
            builder.ToTable("CurriculumFormulas");

            builder.HasKey(f => f.Id);

            builder.HasIndex(f => f.ChapterId);

            builder.Property(f => f.FormulaCode)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(f => f.Title)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(f => f.Latex)
                .IsRequired();

            builder.Property(f => f.Description)
                .HasMaxLength(2000);

            builder.Property(f => f.Requirement)
                .HasMaxLength(1000);

            builder.Property(f => f.Model3D)
                .HasMaxLength(500);

            builder.Property(f => f.ExercisesJson);

            builder.Property(f => f.Order)
                .HasDefaultValue(0);

            builder.Property(f => f.CreatedAt)
                .IsRequired();

            builder.Property(f => f.UpdatedAt)
                .IsRequired();

            builder.HasOne(f => f.Chapter)
                .WithMany(c => c.Formulas)
                .HasForeignKey(f => f.ChapterId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
