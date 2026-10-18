using MathChain.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MathChain.Persistence.Configurations
{
    public class ClassRoomConfiguration : IEntityTypeConfiguration<ClassRoom>
    {
        public void Configure(EntityTypeBuilder<ClassRoom> builder)
        {
            builder.ToTable("ClassRooms");

            builder.HasKey(c => c.Id);

            builder.HasIndex(c => c.JoinCode)
                .IsUnique();

            builder.Property(c => c.JoinCode)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(c => c.TeacherWallet)
                .IsRequired()
                .HasMaxLength(100);
        }
    }
}
