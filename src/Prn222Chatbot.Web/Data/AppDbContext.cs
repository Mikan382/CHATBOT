using Microsoft.EntityFrameworkCore;
using Prn222Chatbot.Web.Domain;

namespace Prn222Chatbot.Web.Data;

public class AppDbContext : DbContext
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
            entity.HasMany(x => x.Chunks).WithOne(x => x.Document).HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
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
            entity.Property(x => x.Status).HasMaxLength(32);
        });

    }
}
