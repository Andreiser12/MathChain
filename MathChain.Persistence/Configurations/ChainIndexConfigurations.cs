using MathChain.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MathChain.Persistence.Configurations
{
    public class ChainCertificateConfiguration : IEntityTypeConfiguration<ChainCertificate>
    {
        public void Configure(EntityTypeBuilder<ChainCertificate> builder)
        {
            builder.ToTable("ChainCertificates");
            builder.HasKey(c => c.TokenId);
            builder.Property(c => c.TokenId).ValueGeneratedNever();
            builder.HasIndex(c => c.Holder);
            builder.Property(c => c.Holder).IsRequired().HasMaxLength(42);
            builder.Property(c => c.Issuer).IsRequired().HasMaxLength(42);
            builder.Property(c => c.SubjectId).IsRequired().HasMaxLength(66);
            builder.Property(c => c.DataHash).IsRequired().HasMaxLength(66);
            builder.Property(c => c.IssuedTransaction).IsRequired().HasMaxLength(66);
            builder.Property(c => c.RevokedBy).HasMaxLength(42);
            builder.Property(c => c.RevokedTransaction).HasMaxLength(66);
        }
    }

    public class ChainSmartAccountConfiguration : IEntityTypeConfiguration<ChainSmartAccount>
    {
        public void Configure(EntityTypeBuilder<ChainSmartAccount> builder)
        {
            builder.ToTable("ChainSmartAccounts");
            builder.HasKey(a => a.Account);
            builder.HasIndex(a => a.Owner);
            builder.Property(a => a.Account).HasMaxLength(42);
            builder.Property(a => a.Owner).IsRequired().HasMaxLength(42);
            builder.Property(a => a.Transaction).IsRequired().HasMaxLength(66);
        }
    }

    public class ChainSolutionPurchaseConfiguration : IEntityTypeConfiguration<ChainSolutionPurchase>
    {
        public void Configure(EntityTypeBuilder<ChainSolutionPurchase> builder)
        {
            builder.ToTable("ChainSolutionPurchases");
            builder.HasKey(p => p.Id);
            builder.HasIndex(p => p.Account);
            builder.Property(p => p.Id).HasMaxLength(80);
            builder.Property(p => p.Account).IsRequired().HasMaxLength(42);
            builder.Property(p => p.SolutionId).IsRequired().HasMaxLength(66);
            builder.Property(p => p.Payer).IsRequired().HasMaxLength(42);
            builder.Property(p => p.Price).HasPrecision(38, 0);
            builder.Property(p => p.Transaction).IsRequired().HasMaxLength(66);
        }
    }

    public class ChainPriceChangeConfiguration : IEntityTypeConfiguration<ChainPriceChange>
    {
        public void Configure(EntityTypeBuilder<ChainPriceChange> builder)
        {
            builder.ToTable("ChainPriceChanges");
            builder.HasKey(p => p.Id);
            builder.Property(p => p.Id).HasMaxLength(80);
            builder.Property(p => p.Price).HasPrecision(38, 0);
            builder.Property(p => p.Transaction).IsRequired().HasMaxLength(66);
        }
    }

    public class ChainCurriculumPublicationConfiguration : IEntityTypeConfiguration<ChainCurriculumPublication>
    {
        public void Configure(EntityTypeBuilder<ChainCurriculumPublication> builder)
        {
            builder.ToTable("ChainCurriculumPublications");
            builder.HasKey(p => p.Version);
            builder.Property(p => p.Version).ValueGeneratedNever();
            builder.Property(p => p.Cid).IsRequired().HasMaxLength(100);
            builder.Property(p => p.Publisher).IsRequired().HasMaxLength(42);
            builder.Property(p => p.Transaction).IsRequired().HasMaxLength(66);
        }
    }

    public class IndexerCheckpointConfiguration : IEntityTypeConfiguration<IndexerCheckpoint>
    {
        public void Configure(EntityTypeBuilder<IndexerCheckpoint> builder)
        {
            builder.ToTable("IndexerCheckpoints");
            builder.HasKey(c => c.Id);
            builder.Property(c => c.Id).HasMaxLength(50);
        }
    }
}
