using MathChain.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MathChain.Persistence.Configurations
{
    public class ExternalLoginConfiguration : IEntityTypeConfiguration<ExternalLogin>
    {
        public void Configure(EntityTypeBuilder<ExternalLogin> builder)
        {
            builder.ToTable("ExternalLogins");

            builder.HasKey(l => l.Id);

            builder.HasIndex(l => new { l.Provider, l.Subject })
                .IsUnique();

            builder.Property(l => l.Provider)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(l => l.Subject)
                .IsRequired()
                .HasMaxLength(255);

            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
