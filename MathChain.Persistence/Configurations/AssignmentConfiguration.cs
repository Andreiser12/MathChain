using MathChain.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MathChain.Persistence.Configurations
{
    public class AssignmentConfiguration : IEntityTypeConfiguration<AssignmentBase>
    {
        public void Configure(EntityTypeBuilder<AssignmentBase> builder)
        {
            builder.ToTable("Assignments");

            builder.HasKey(a => a.Id);

            builder.Property(a => a.Title)
                .IsRequired()
                .HasMaxLength(200);
        }
    }
}
