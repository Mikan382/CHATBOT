using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentChunkingStrategy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Existing documents were indexed before the strategy was tracked, so mark them "unknown".
            migrationBuilder.AddColumn<string>(
                name: "ChunkingStrategy",
                table: "Documents",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "unknown");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChunkingStrategy",
                table: "Documents");
        }
    }
}
