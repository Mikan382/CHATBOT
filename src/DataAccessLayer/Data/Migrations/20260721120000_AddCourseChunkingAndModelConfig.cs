using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseChunkingAndModelConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DefaultChunkingStrategy",
                table: "Courses",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DefaultChunkSize",
                table: "Courses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DefaultChunkOverlap",
                table: "Courses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultEmbeddingModel",
                table: "Courses",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultChunkingStrategy",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "DefaultChunkSize",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "DefaultChunkOverlap",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "DefaultEmbeddingModel",
                table: "Courses");
        }
    }
}
