using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLegacyRetrievalAndSnapshotEntitlements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StudentSubscriptions_StudentUserId_Pending",
                table: "StudentSubscriptions");

            migrationBuilder.DropColumn(
                name: "NormalizedContent",
                table: "DocumentChunks");

            migrationBuilder.AddColumn<int>(
                name: "MessageQuotaAtActivation",
                table: "StudentSubscriptions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DurationDays",
                table: "PaymentTransactions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MessageQuota",
                table: "PaymentTransactions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                """
                UPDATE subscriptions
                SET subscriptions.MessageQuotaAtActivation = plans.MessageQuota
                FROM StudentSubscriptions AS subscriptions
                INNER JOIN SubscriptionPlans AS plans
                    ON plans.Id = subscriptions.SubscriptionPlanId;

                UPDATE payments
                SET payments.DurationDays = plans.DurationDays,
                    payments.MessageQuota = plans.MessageQuota
                FROM PaymentTransactions AS payments
                INNER JOIN SubscriptionPlans AS plans
                    ON plans.Id = payments.SubscriptionPlanId;

                UPDATE StudentSubscriptions
                SET Status = 'Cancelled',
                    UpdatedAtUtc = SYSUTCDATETIME()
                WHERE Status IN ('Pending', 'Rejected');
                """);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_Status_PaidAtUtc",
                table: "PaymentTransactions",
                columns: new[] { "Status", "PaidAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_SubscriptionPlanId",
                table: "PaymentTransactions",
                column: "SubscriptionPlanId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PaymentTransactions_Status_PaidAtUtc",
                table: "PaymentTransactions");

            migrationBuilder.DropIndex(
                name: "IX_PaymentTransactions_SubscriptionPlanId",
                table: "PaymentTransactions");

            migrationBuilder.DropColumn(
                name: "MessageQuotaAtActivation",
                table: "StudentSubscriptions");

            migrationBuilder.DropColumn(
                name: "DurationDays",
                table: "PaymentTransactions");

            migrationBuilder.DropColumn(
                name: "MessageQuota",
                table: "PaymentTransactions");

            migrationBuilder.AddColumn<string>(
                name: "NormalizedContent",
                table: "DocumentChunks",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(
                "UPDATE DocumentChunks SET NormalizedContent = LOWER(Content);");

            migrationBuilder.CreateIndex(
                name: "IX_StudentSubscriptions_StudentUserId_Pending",
                table: "StudentSubscriptions",
                column: "StudentUserId",
                unique: true,
                filter: "[Status] = 'Pending'");
        }
    }
}
