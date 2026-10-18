using MathChain.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MathChain.Persistence.Configurations
{
    public class SubmissionConfiguration : IEntityTypeConfiguration<Submission>
    {
        public void Configure(EntityTypeBuilder<Submission> builder)
        {
            builder.ToTable("Submissions");

            builder.HasKey(s => s.Id);

            builder.Property(s => s.StudentWallet)
                .IsRequired()
                .HasMaxLength(100);
        }
    }
}
