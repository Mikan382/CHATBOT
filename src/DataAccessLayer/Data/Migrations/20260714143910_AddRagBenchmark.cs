using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRagBenchmark : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BenchmarkRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CourseName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ChunkingStrategy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EmbeddingModelName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    TopK = table.Column<int>(type: "int", nullable: false),
                    QuestionCount = table.Column<int>(type: "int", nullable: false),
                    ChunkCount = table.Column<int>(type: "int", nullable: false),
                    DurationMilliseconds = table.Column<long>(type: "bigint", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BenchmarkRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EvaluationQuestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExpectedDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExpectedSourceName = table.Column<string>(type: "nvarchar(520)", maxLength: 520, nullable: false),
                    Question = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ExpectedAnswer = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluationQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EvaluationQuestions_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BenchmarkResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BenchmarkRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EvaluationQuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    Question = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ExpectedAnswer = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExpectedDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExpectedSourceName = table.Column<string>(type: "nvarchar(520)", maxLength: 520, nullable: false),
                    GeneratedAnswer = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RetrievedSourcesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HitAtK = table.Column<bool>(type: "bit", nullable: false),
                    ReciprocalRank = table.Column<double>(type: "float", nullable: false),
                    AnswerTokenF1 = table.Column<double>(type: "float", nullable: false),
                    LatencyMilliseconds = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BenchmarkResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BenchmarkResults_BenchmarkRuns_BenchmarkRunId",
                        column: x => x.BenchmarkRunId,
                        principalTable: "BenchmarkRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BenchmarkResults_BenchmarkRunId_DisplayOrder",
                table: "BenchmarkResults",
                columns: new[] { "BenchmarkRunId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_BenchmarkResults_EvaluationQuestionId",
                table: "BenchmarkResults",
                column: "EvaluationQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_BenchmarkRuns_ChunkingStrategy_EmbeddingModelName_CompletedAtUtc",
                table: "BenchmarkRuns",
                columns: new[] { "ChunkingStrategy", "EmbeddingModelName", "CompletedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_BenchmarkRuns_CourseId_CompletedAtUtc",
                table: "BenchmarkRuns",
                columns: new[] { "CourseId", "CompletedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationQuestions_CourseId_IsActive_DisplayOrder",
                table: "EvaluationQuestions",
                columns: new[] { "CourseId", "IsActive", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationQuestions_ExpectedDocumentId",
                table: "EvaluationQuestions",
                column: "ExpectedDocumentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BenchmarkResults");

            migrationBuilder.DropTable(
                name: "EvaluationQuestions");

            migrationBuilder.DropTable(
                name: "BenchmarkRuns");
        }
    }
}
