using MathChain.Domain.Entities;
using Microsoft.EntityFrameworkCore;

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
        public DbSet<CurriculumFormula> CurriculumFormulas { get; set; }
        public DbSet<ChapterPrerequisite> ChapterPrerequisites { get; set; }
        public DbSet<ContentBlock> ContentBlocks { get; set; }
        public DbSet<ChainCertificate> ChainCertificates { get; set; }
        public DbSet<ChainSmartAccount> ChainSmartAccounts { get; set; }
        public DbSet<ChainSolutionPurchase> ChainSolutionPurchases { get; set; }
        public DbSet<ChainPriceChange> ChainPriceChanges { get; set; }
        public DbSet<ChainCurriculumPublication> ChainCurriculumPublications { get; set; }
        public DbSet<IndexerCheckpoint> IndexerCheckpoints { get; set; }
        public DbSet<ExternalLogin> ExternalLogins { get; set; }
        public DbSet<Certificate> Certificates { get; set; }
        public DbSet<PracticeExercise> PracticeExercises { get; set; }
        public DbSet<SolutionPurchase> SolutionPurchases { get; set; }

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
