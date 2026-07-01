using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddResearchBenchmarkFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ChunkingStrategy",
                table: "EvaluationResults",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "paragraph");

            migrationBuilder.AddColumn<string>(
                name: "EmbeddingModelName",
                table: "EvaluationResults",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "RagLatencyMs",
                table: "EvaluationResults",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FineTunedLatencyMs",
                table: "EvaluationResults",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "FtFaithfulness",
                table: "EvaluationResults",
                type: "decimal(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "FtAnswerRelevance",
                table: "EvaluationResults",
                type: "decimal(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChunkingStrategy",
                table: "EvaluationResults");

            migrationBuilder.DropColumn(
                name: "EmbeddingModelName",
                table: "EvaluationResults");

            migrationBuilder.DropColumn(
                name: "RagLatencyMs",
                table: "EvaluationResults");

            migrationBuilder.DropColumn(
                name: "FineTunedLatencyMs",
                table: "EvaluationResults");

            migrationBuilder.DropColumn(
                name: "FtFaithfulness",
                table: "EvaluationResults");

            migrationBuilder.DropColumn(
                name: "FtAnswerRelevance",
                table: "EvaluationResults");
        }
    }
}
