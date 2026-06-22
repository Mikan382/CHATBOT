using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using DataAccessLayer.Entities;

namespace DataAccessLayer.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Chapter> Chapters => Set<Chapter>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();
    public DbSet<DocumentChunkEmbedding> DocumentChunkEmbeddings => Set<DocumentChunkEmbedding>();
    public DbSet<ChatSession> ChatSessions => Set<ChatSession>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<EvaluationQuestion> EvaluationQuestions => Set<EvaluationQuestion>();
    public DbSet<EvaluationResult> EvaluationResults => Set<EvaluationResult>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Course>(entity =>
        {
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(32);
            entity.Property(x => x.Name).HasMaxLength(256);
            entity.Property(x => x.Tools).HasMaxLength(512);
        });

        modelBuilder.Entity<Chapter>(entity =>
        {
            entity.HasIndex(x => new { x.CourseId, x.Order }).IsUnique();
            entity.Property(x => x.Clo).HasMaxLength(32);
            entity.Property(x => x.Title).HasMaxLength(256);
        });

        modelBuilder.Entity<Document>(entity =>
        {
            entity.Property(x => x.OriginalFileName).HasMaxLength(260);
            entity.Property(x => x.FileType).HasMaxLength(16);
            entity.Property(x => x.IndexStatus).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.IndexStage).HasMaxLength(160);
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
            entity.Property(x => x.ModelType).HasConversion<string>().HasMaxLength(32);
        });

        modelBuilder.Entity<EvaluationQuestion>(entity =>
        {
            entity.HasIndex(x => x.Order).IsUnique();
        });

        modelBuilder.Entity<EvaluationResult>(entity =>
        {
            entity.Property(x => x.Faithfulness).HasPrecision(5, 4);
            entity.Property(x => x.AnswerRelevance).HasPrecision(5, 4);
            entity.Property(x => x.RetrievalRecall).HasPrecision(5, 4);
            entity.Property(x => x.CitationAccuracy).HasPrecision(5, 4);
            entity.Property(x => x.FtFaithfulness).HasPrecision(5, 4);
            entity.Property(x => x.FtAnswerRelevance).HasPrecision(5, 4);
            entity.Property(x => x.Status).HasMaxLength(32);
            entity.Property(x => x.ChunkingStrategy).HasMaxLength(64).HasDefaultValue("paragraph");
            entity.Property(x => x.EmbeddingModelName).HasMaxLength(160);
        });

        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(x => x.FullName).HasMaxLength(160);
        });

    }
}
