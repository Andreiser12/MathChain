using MathChain.Domain.Entities;
using MathChain.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MathChain.Persistence.Configurations
{
    public class SolutionPurchaseConfiguration : IEntityTypeConfiguration<SolutionPurchase>
    {
        public void Configure(EntityTypeBuilder<SolutionPurchase> builder)
        {
            builder.ToTable("SolutionPurchases");

            builder.HasKey(p => p.Id);

            builder.HasIndex(p => new { p.AccountWallet, p.FormulaId, p.ExerciseIndex }, "IX_SolutionPurchases_Account");

            builder.HasIndex(p => new { p.AccountWallet, p.FormulaId, p.ExerciseIndex }, "IX_SolutionPurchases_Completed")
                .IsUnique()
                .HasFilter($"\"Status\" = {(int)SolutionPurchaseStatus.Completed}");

            builder.HasIndex(p => p.TransactionHash)
                .IsUnique()
                .HasFilter("\"TransactionHash\" IS NOT NULL");

            builder.Property(p => p.AccountWallet)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(p => p.PayerAddress)
                .IsRequired()
                .HasMaxLength(42);

            builder.Property(p => p.SolutionId)
                .IsRequired()
                .HasMaxLength(66);

            builder.Property(p => p.Salt)
                .IsRequired()
                .HasMaxLength(66);

            builder.Property(p => p.Nonce)
                .IsRequired()
                .HasMaxLength(66);

            builder.Property(p => p.TransactionHash)
                .HasMaxLength(66);

            builder.Property(p => p.FailureReason)
                .HasMaxLength(500);
        }
    }
}
