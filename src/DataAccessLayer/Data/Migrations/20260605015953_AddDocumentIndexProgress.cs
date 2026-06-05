using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentIndexProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IndexProgressPercent",
                table: "Documents",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "IndexStage",
                table: "Documents",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE Documents
                SET
                    IndexProgressPercent = CASE
                        WHEN IndexStatus = N'Indexed' THEN 100
                        WHEN IndexStatus = N'Processing' THEN 10
                        ELSE 0
                    END,
                    IndexStage = CASE
                        WHEN IndexStatus = N'Indexed' THEN N'Indexed'
                        WHEN IndexStatus = N'Processing' THEN N'Processing'
                        WHEN IndexStatus = N'Failed' THEN N'Failed'
                        ELSE N'Queued'
                    END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IndexProgressPercent",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "IndexStage",
                table: "Documents");
        }
    }
}
