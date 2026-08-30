using MathChain.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography.X509Certificates;

namespace MathChain.API.Data
{
    public class MathChainDbContext : DbContext
    {
        public MathChainDbContext(DbContextOptions<MathChainDbContext> options) : base(options)
        {
        }
        public DbSet<User> Users { get; set; }
        public DbSet<ClassRoom> ClassRooms { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }
        public DbSet<AssignmentBase> Assignmentss { get; set; }
        public DbSet<Submission> Submissions { get; set; }
        public DbSet<Grade> Grades { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<AssignmentBase>()
                .HasDiscriminator<string>("AssignmentType")
                .HasValue<Homework>("Homework")
                .HasValue<Test>("Test");

            modelBuilder.Entity<User>()
                .HasIndex(u => u.WalletAddress)
                .IsUnique();

            modelBuilder.Entity<ClassRoom>()
                .HasIndex(c => c.JoinCode)
                .IsUnique();
        }

    }
}
