using MathChain.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MathChain.Persistence.Configurations
{
    public class ClassMediaConfiguration : IEntityTypeConfiguration<ClassMedia>
    {
        public void Configure(EntityTypeBuilder<ClassMedia> builder)
        {
            builder.ToTable("ClassMedia");

            builder.HasKey(m => m.Id);
        }
    }
    public class ClassResourceConfiguration : IEntityTypeConfiguration<ClassResource>
    {
        public void Configure(EntityTypeBuilder<ClassResource> builder)
        {
            builder.ToTable("ClassMaterials");

            builder.HasKey(r => r.Id);
        }
    }
    public class ClassAnnouncementConfiguration : IEntityTypeConfiguration<ClassAnnouncement>
    {
        public void Configure(EntityTypeBuilder<ClassAnnouncement> builder)
        {
            builder.ToTable("ClassAnnouncements");

            builder.HasKey(a => a.Id);
        }
    }
    public class MessageCommentConfiguration : IEntityTypeConfiguration<MessageComment>
    {
        public void Configure(EntityTypeBuilder<MessageComment> builder)
        {
            builder.ToTable("MessagesComments");

            builder.HasKey(c => c.Id);
        }
    }
}
