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
            entity.Property(x => x.Code).HasMaxLength(32);
            entity.Property(x => x.Name).HasMaxLength(160);
            entity.Property(x => x.Description).HasMaxLength(600);
            entity.Property(x => x.MonthlyPrice).HasPrecision(18, 2);
        });

        modelBuilder.Entity<StudentSubscription>(entity =>
        {
            entity.HasIndex(x => x.StudentUserId)
                .HasFilter("[Status] = 'Active'")
                .IsUnique();
            entity.HasIndex(x => new { x.SubscriptionPlanId, x.Status });
            entity.Property(x => x.Status).HasMaxLength(32);
            entity.HasOne(x => x.Student)
                .WithMany()
                .HasForeignKey(x => x.StudentUserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Plan)
                .WithMany(x => x.Subscriptions)
                .HasForeignKey(x => x.SubscriptionPlanId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
