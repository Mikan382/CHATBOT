using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddChatQuotaAndPriceSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MessageQuota",
                table: "SubscriptionPlans",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "PriceAtActivation",
                table: "StudentSubscriptions",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            // Snapshot the current plan price onto existing subscriptions so historical rows
            // keep the price they were activated at instead of following later plan edits.
            migrationBuilder.Sql(@"
UPDATE ss
SET ss.PriceAtActivation = sp.MonthlyPrice
FROM StudentSubscriptions ss
INNER JOIN SubscriptionPlans sp ON sp.Id = ss.SubscriptionPlanId;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MessageQuota",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "PriceAtActivation",
                table: "StudentSubscriptions");
        }
    }
}
