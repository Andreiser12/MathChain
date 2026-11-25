using MathChain.Domain.Entities;
using MathChain.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MathChain.Persistence.Configurations
{
    public class CertificateConfiguration : IEntityTypeConfiguration<Certificate>
    {
        public void Configure(EntityTypeBuilder<Certificate> builder)
        {
            builder.ToTable("Certificates");

            builder.HasKey(c => c.Id);

            builder.HasIndex(c => c.TokenId)
                .IsUnique();

            builder.HasIndex(c => new { c.Kind, c.SubjectId, c.StudentWallet })
                .IsUnique()
                .HasFilter($"\"Status\" <> {(int)CertificateStatus.Revoked}");

            builder.HasIndex(c => c.StudentWallet);

            builder.Property(c => c.SubjectTitle)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(c => c.StudentWallet)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(c => c.HolderAddress)
                .HasMaxLength(42);

            builder.Property(c => c.TeacherWallet)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(c => c.Salt)
                .IsRequired()
                .HasMaxLength(66);

            builder.Property(c => c.DataHash)
                .IsRequired()
                .HasMaxLength(66);

            builder.Property(c => c.TransactionHash)
                .HasMaxLength(66);
        }
    }
}
