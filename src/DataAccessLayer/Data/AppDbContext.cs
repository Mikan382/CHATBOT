using Microsoft.EntityFrameworkCore;
using DataAccessLayer.Entities;

namespace DataAccessLayer.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Course> Courses => Set<Course>();
    public DbSet<ApplicationUser> Users => Set<ApplicationUser>();
    public DbSet<CourseTeacher> CourseTeachers => Set<CourseTeacher>();
    public DbSet<Chapter> Chapters => Set<Chapter>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();
    public DbSet<DocumentChunkEmbedding> DocumentChunkEmbeddings => Set<DocumentChunkEmbedding>();
    public DbSet<ChatSession> ChatSessions => Set<ChatSession>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();
    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
    public DbSet<StudentSubscription> StudentSubscriptions => Set<StudentSubscription>();
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();
    public DbSet<EvaluationQuestion> EvaluationQuestions => Set<EvaluationQuestion>();
    public DbSet<BenchmarkRun> BenchmarkRuns => Set<BenchmarkRun>();
    public DbSet<BenchmarkResult> BenchmarkResults => Set<BenchmarkResult>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Course>(entity =>
        {
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(32);
            entity.Property(x => x.Name).HasMaxLength(256);
            entity.Property(x => x.Tools).HasMaxLength(512);
        });

        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("ApplicationUsers");
            entity.HasIndex(x => x.Email).IsUnique();
            entity.Property(x => x.Email).HasMaxLength(256);
            entity.Property(x => x.DisplayName).HasMaxLength(160);
            entity.Property(x => x.PasswordHash).HasMaxLength(512);
            entity.Property(x => x.Role).HasMaxLength(32);
        });

        modelBuilder.Entity<CourseTeacher>(entity =>
        {
            entity.HasKey(x => new { x.CourseId, x.TeacherUserId });
            entity.HasOne(x => x.Course)
                .WithMany(x => x.TeacherAssignments)
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Teacher)
                .WithMany(x => x.TeachingAssignments)
                .HasForeignKey(x => x.TeacherUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Chapter>(entity =>
        {
            entity.HasIndex(x => new { x.CourseId, x.Order }).IsUnique();
            entity.Property(x => x.Clo).HasMaxLength(32);
            entity.Property(x => x.Title).HasMaxLength(256);
        });

        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasIndex(x => new { x.ChapterId, x.ContentHash }).IsUnique();
            entity.Property(x => x.OriginalFileName).HasMaxLength(260);
            entity.Property(x => x.FileType).HasMaxLength(16);
            entity.Property(x => x.ContentHash).HasMaxLength(64);
            entity.Property(x => x.ChunkingStrategy).HasMaxLength(64);
            entity.HasMany(x => x.Chunks).WithOne(x => x.Document).HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.UploadedByUser)
                .WithMany()
                .HasForeignKey(x => x.UploadedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<DocumentChunk>(entity =>
        {
            entity.HasIndex(x => new { x.DocumentId, x.ChunkIndex }).IsUnique();
            entity.Property(x => x.SourceName).HasMaxLength(260);
        });

        modelBuilder.Entity<DocumentChunkEmbedding>(entity =>
        {
            entity.HasIndex(x => new { x.DocumentChunkId, x.ModelName }).IsUnique();
            entity.Property(x => x.ModelName).HasMaxLength(160);
            entity.HasOne(x => x.DocumentChunk)
                .WithMany(x => x.Embeddings)
                .HasForeignKey(x => x.DocumentChunkId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ChatSession>(entity =>
        {
            entity.Property(x => x.Title).HasMaxLength(160);
            entity.HasMany(x => x.Messages).WithOne(x => x.ChatSession).HasForeignKey(x => x.ChatSessionId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.Course)
                .WithMany()
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.Property(x => x.Role).HasConversion<string>().HasMaxLength(32);
        });

        modelBuilder.Entity<SystemSetting>(entity =>
        {
            entity.HasKey(x => x.Key);
            entity.Property(x => x.Key).HasMaxLength(80);
            entity.Property(x => x.Value).HasMaxLength(256);
        });

        modelBuilder.Entity<SubscriptionPlan>(entity =>
        {
            entity.HasIndex(x => x.Code).IsUnique();
            entity.HasIndex(x => x.IsDefault)
                .HasFilter("[IsDefault] = 1")
                .IsUnique();
            entity.Property(x => x.Code).HasMaxLength(32);
            entity.Property(x => x.Name).HasMaxLength(160);
            entity.Property(x => x.Description).HasMaxLength(600);
            entity.Property(x => x.Price).HasPrecision(18, 2);
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("CK_SubscriptionPlans_Price", "[Price] >= 0");
                table.HasCheckConstraint("CK_SubscriptionPlans_DurationDays", "[DurationDays] > 0");
                table.HasCheckConstraint("CK_SubscriptionPlans_TokenQuota", "[TokenQuota] > 0");
                table.HasCheckConstraint(
                    "CK_SubscriptionPlans_Default",
                    "[IsDefault] = 0 OR ([IsActive] = 1 AND [Price] = 0)");
            });
        });

        modelBuilder.Entity<StudentSubscription>(entity =>
        {
            entity.HasIndex(x => x.StudentUserId, "IX_StudentSubscriptions_StudentUserId")
                .HasFilter("[Status] = 'Active'")
                .IsUnique();
            entity.HasIndex(x => new { x.SubscriptionPlanId, x.Status });
            entity.Property(x => x.Status).HasMaxLength(32);
            entity.Property(x => x.PriceAtActivation).HasPrecision(18, 2);
            entity.ToTable(table =>
            {
                table.HasCheckConstraint(
                    "CK_StudentSubscriptions_TokenQuotaAtActivation",
                    "[TokenQuotaAtActivation] > 0");
                table.HasCheckConstraint(
                    "CK_StudentSubscriptions_TokenUsage",
                    "[InputTokensUsed] >= 0 AND [OutputTokensUsed] >= 0 AND [TotalTokensUsed] >= 0");
            });
            entity.HasOne(x => x.Student)
                .WithMany()
                .HasForeignKey(x => x.StudentUserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Plan)
                .WithMany(x => x.Subscriptions)
                .HasForeignKey(x => x.SubscriptionPlanId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PaymentTransaction>(entity =>
        {
            // ProviderTxnRef is the idempotency anchor: the gateway can fire the return + IPN
            // callbacks more than once, and the unique index guarantees a single Paid row per ref.
            entity.HasIndex(x => x.ProviderTxnRef).IsUnique();
            entity.HasIndex(x => x.StudentUserId);
            entity.HasIndex(x => new { x.Status, x.PaidAtUtc });
            entity.Property(x => x.Provider).HasMaxLength(32);
            entity.Property(x => x.ProviderTxnRef).HasMaxLength(64);
            entity.Property(x => x.Status).HasMaxLength(32);
            entity.Property(x => x.ProviderTransactionNo).HasMaxLength(64);
            entity.Property(x => x.ResponseCode).HasMaxLength(16);
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.HasOne(x => x.Student)
                .WithMany()
                .HasForeignKey(x => x.StudentUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Plan)
                .WithMany()
                .HasForeignKey(x => x.SubscriptionPlanId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EvaluationQuestion>(entity =>
        {
            // Source IDs are validated by BenchmarkService; the source label is snapshotted so question history survives document changes.
            entity.HasIndex(x => new { x.CourseId, x.IsActive, x.DisplayOrder });
            entity.HasIndex(x => x.ExpectedDocumentId);
            entity.Property(x => x.ExpectedSourceName).HasMaxLength(520);
            entity.Property(x => x.Question).HasMaxLength(2000);
            entity.HasOne(x => x.Course)
                .WithMany()
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BenchmarkRun>(entity =>
        {
            entity.HasIndex(x => new { x.CourseId, x.CompletedAtUtc });
            entity.HasIndex(x => new { x.ChunkingStrategy, x.EmbeddingModelName, x.CompletedAtUtc });
            entity.Property(x => x.CourseCode).HasMaxLength(32);
            entity.Property(x => x.CourseName).HasMaxLength(256);
            entity.Property(x => x.ChunkingStrategy).HasMaxLength(64);
            entity.Property(x => x.EmbeddingModelName).HasMaxLength(160);
            entity.HasMany(x => x.Results)
                .WithOne(x => x.BenchmarkRun)
                .HasForeignKey(x => x.BenchmarkRunId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BenchmarkResult>(entity =>
        {
            // Question/source IDs are historical references, not cascading foreign keys.
            entity.HasIndex(x => new { x.BenchmarkRunId, x.DisplayOrder });
            entity.HasIndex(x => x.EvaluationQuestionId);
            entity.Property(x => x.ExpectedSourceName).HasMaxLength(520);
            entity.Property(x => x.Question).HasMaxLength(2000);
        });
    }
}
