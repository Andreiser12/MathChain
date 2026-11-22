using MathChain.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MathChain.Persistence.Configurations
{
    public class PracticeExerciseConfiguration : IEntityTypeConfiguration<PracticeExercise>
    {
        public void Configure(EntityTypeBuilder<PracticeExercise> builder)
        {
            builder.ToTable("PracticeExercises");

            builder.HasKey(e => e.Id);

            builder.HasIndex(e => new { e.StudentWallet, e.FormulaId, e.ExerciseIndex });

            builder.Property(e => e.StudentWallet)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(e => e.Difficulty)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(e => e.Requirement)
                .IsRequired()
                .HasMaxLength(1000);

            builder.Property(e => e.Latex)
                .IsRequired()
                .HasMaxLength(2000);

            builder.Property(e => e.Hint)
                .HasMaxLength(1000);

            builder.Property(e => e.ExpectedAnswer)
                .IsRequired()
                .HasMaxLength(1000);

            builder.Property(e => e.VariableValues)
                .HasMaxLength(500);
        }
    }
}
