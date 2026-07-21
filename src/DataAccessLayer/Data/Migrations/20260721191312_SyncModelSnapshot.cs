using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Data.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Guarded SQL on purpose: part of this schema used to be applied by raw
            // bootstrapper SQL instead of a migration, so existing databases already have
            // objects that a fresh database does not. Plain AddColumn/DropTable would fail
            // on one side or the other; these statements are safe to run on both.

            // CourseTeachers.IsHead only ever existed through bootstrapper SQL.
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[CourseTeachers]') AND name = N'IsHead')
BEGIN
    ALTER TABLE [CourseTeachers] ADD [IsHead] bit NOT NULL DEFAULT 0;
END");

            // Chunks that belonged to student attachments have no document owner. They are
            // dead data now that the feature is gone; their embeddings cascade with them.
            migrationBuilder.Sql(@"
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[DocumentChunks]') AND name = N'StudentDocumentId')
BEGIN
    DELETE FROM [DocumentChunks] WHERE [DocumentId] IS NULL;
END");

            // Drop the student attachment schema.
            migrationBuilder.Sql(@"
IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = N'FK_DocumentChunks_StudentUploadedDocuments_StudentDocumentId')
BEGIN
    ALTER TABLE [DocumentChunks] DROP CONSTRAINT [FK_DocumentChunks_StudentUploadedDocuments_StudentDocumentId];
END

IF EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_DocumentChunks_StudentDocumentId' AND object_id = OBJECT_ID(N'[DocumentChunks]'))
BEGIN
    DROP INDEX [IX_DocumentChunks_StudentDocumentId] ON [DocumentChunks];
END

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[DocumentChunks]') AND name = N'StudentDocumentId')
BEGIN
    ALTER TABLE [DocumentChunks] DROP COLUMN [StudentDocumentId];
END

IF OBJECT_ID(N'[StudentUploadedDocuments]') IS NOT NULL
BEGIN
    DROP TABLE [StudentUploadedDocuments];
END");

            // The model keeps DocumentId nullable; align databases that still have it NOT NULL.
            migrationBuilder.Sql(@"
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[DocumentChunks]') AND name = N'DocumentId' AND is_nullable = 0)
BEGIN
    IF EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_DocumentChunks_DocumentId_ChunkIndex' AND object_id = OBJECT_ID(N'[DocumentChunks]'))
        DROP INDEX [IX_DocumentChunks_DocumentId_ChunkIndex] ON [DocumentChunks];

    ALTER TABLE [DocumentChunks] ALTER COLUMN [DocumentId] uniqueidentifier NULL;
END");

            // (DocumentId, ChunkIndex) must not be unique: re-indexing a document rewrites
            // its chunks, and the model declares this index without IsUnique.
            migrationBuilder.Sql(@"
IF EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_DocumentChunks_DocumentId_ChunkIndex' AND object_id = OBJECT_ID(N'[DocumentChunks]') AND is_unique = 1)
BEGIN
    DROP INDEX [IX_DocumentChunks_DocumentId_ChunkIndex] ON [DocumentChunks];
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_DocumentChunks_DocumentId_ChunkIndex' AND object_id = OBJECT_ID(N'[DocumentChunks]'))
BEGIN
    CREATE INDEX [IX_DocumentChunks_DocumentId_ChunkIndex] ON [DocumentChunks] ([DocumentId], [ChunkIndex]);
END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Only the index shape is restored. The student attachment schema and the
            // deleted chunk rows are intentionally not recreated.
            migrationBuilder.Sql(@"
IF EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_DocumentChunks_DocumentId_ChunkIndex' AND object_id = OBJECT_ID(N'[DocumentChunks]'))
BEGIN
    DROP INDEX [IX_DocumentChunks_DocumentId_ChunkIndex] ON [DocumentChunks];
END

CREATE UNIQUE INDEX [IX_DocumentChunks_DocumentId_ChunkIndex] ON [DocumentChunks] ([DocumentId], [ChunkIndex]);");
        }
    }
}
