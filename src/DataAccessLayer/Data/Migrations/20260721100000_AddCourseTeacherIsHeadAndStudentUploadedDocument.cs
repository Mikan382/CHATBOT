using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseTeacherIsHeadAndStudentUploadedDocument : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsHead",
                table: "CourseTeachers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<Guid>(
                name: "DocumentId",
                table: "DocumentChunks",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<Guid>(
                name: "StudentDocumentId",
                table: "DocumentChunks",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "StudentUploadedDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChatSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    FileType = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    ContentText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ChunkingStrategy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    UploadedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentUploadedDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentUploadedDocuments_ApplicationUsers_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_StudentUploadedDocuments_ChatSessions_ChatSessionId",
                        column: x => x.ChatSessionId,
                        principalTable: "ChatSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentChunks_StudentDocumentId",
                table: "DocumentChunks",
                column: "StudentDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentUploadedDocuments_ChatSessionId",
                table: "StudentUploadedDocuments",
                column: "ChatSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentUploadedDocuments_UploadedByUserId",
                table: "StudentUploadedDocuments",
                column: "UploadedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentChunks_StudentUploadedDocuments_StudentDocumentId",
                table: "DocumentChunks",
                column: "StudentDocumentId",
                principalTable: "StudentUploadedDocuments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DocumentChunks_StudentUploadedDocuments_StudentDocumentId",
                table: "DocumentChunks");

            migrationBuilder.DropTable(
                name: "StudentUploadedDocuments");

            migrationBuilder.DropIndex(
                name: "IX_DocumentChunks_StudentDocumentId",
                table: "DocumentChunks");

            migrationBuilder.DropColumn(
                name: "StudentDocumentId",
                table: "DocumentChunks");

            migrationBuilder.DropColumn(
                name: "IsHead",
                table: "CourseTeachers");

            migrationBuilder.AlterColumn<Guid>(
                name: "DocumentId",
                table: "DocumentChunks",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);
        }
    }
}
