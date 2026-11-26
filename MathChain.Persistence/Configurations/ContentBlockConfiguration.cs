using MathChain.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MathChain.Persistence.Configurations
{
    public class ContentBlockConfiguration : IEntityTypeConfiguration<ContentBlock>
    {
        public void Configure(EntityTypeBuilder<ContentBlock> builder)
        {
            builder.ToTable("ContentBlocks");

            builder.HasKey(b => b.Cid);

            builder.Property(b => b.Cid)
                .HasMaxLength(64);

            builder.Property(b => b.Data)
                .IsRequired();

            builder.Property(b => b.CreatedAt)
                .IsRequired();
        }
    }
}
