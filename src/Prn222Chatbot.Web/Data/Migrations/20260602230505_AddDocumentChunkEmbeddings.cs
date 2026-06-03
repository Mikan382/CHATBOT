using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prn222Chatbot.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentChunkEmbeddings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DocumentChunkEmbeddings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentChunkId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModelName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Dimensions = table.Column<int>(type: "int", nullable: false),
                    VectorJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentChunkEmbeddings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentChunkEmbeddings_DocumentChunks_DocumentChunkId",
                        column: x => x.DocumentChunkId,
                        principalTable: "DocumentChunks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentChunkEmbeddings_DocumentChunkId_ModelName",
                table: "DocumentChunkEmbeddings",
                columns: new[] { "DocumentChunkId", "ModelName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentChunkEmbeddings");
        }
    }
}
