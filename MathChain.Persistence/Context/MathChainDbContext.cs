using MathChain.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathChain.Persistence.Context
{
    public class MathChainDbContext : DbContext
    {
        public MathChainDbContext(DbContextOptions<MathChainDbContext> options) : base(options)
        {

        }

        public DbSet<User> Users { get; set; }
        public DbSet<ClassRoom> ClassRooms { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }
        public DbSet<AssignmentBase> Assignments { get; set; }
        public DbSet<Submission> Submissions { get; set; }
        public DbSet<Grade> Grades { get; set; }
        public DbSet<ClassResource> ClassResources { get; set; }
        public DbSet<ClassAnnouncement> ClassAnnouncements { get; set; }
        public DbSet<MessageComment> MessagesComments { get; set; }
        public DbSet<ClassMedia> ClassMedia { get; set; }
        public DbSet<CurriculumChapter> CurriculumChapters { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<AssignmentBase>()
                .HasDiscriminator<string>("AssignmentType")
                .HasValue<Assignment>("Homework")
                .HasValue<Test>("Test");

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(MathChainDbContext).Assembly);
        }
    }
}
