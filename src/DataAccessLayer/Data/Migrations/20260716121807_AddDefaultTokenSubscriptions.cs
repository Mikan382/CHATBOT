using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDefaultTokenSubscriptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatMessageUsages");

            migrationBuilder.RenameColumn(
                name: "MessageQuota",
                table: "SubscriptionPlans",
                newName: "TokenQuota");

            migrationBuilder.RenameColumn(
                name: "MessageQuotaAtActivation",
                table: "StudentSubscriptions",
                newName: "TokenQuotaAtActivation");

            migrationBuilder.RenameColumn(
                name: "MessageQuota",
                table: "PaymentTransactions",
                newName: "TokenQuota");

            migrationBuilder.RenameColumn(
                name: "MonthlyPrice",
                table: "SubscriptionPlans",
                newName: "Price");

            migrationBuilder.AlterColumn<long>(
                name: "TokenQuota",
                table: "SubscriptionPlans",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<long>(
                name: "TokenQuotaAtActivation",
                table: "StudentSubscriptions",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<long>(
                name: "TokenQuota",
                table: "PaymentTransactions",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "SubscriptionPlans",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "InputTokensUsed",
                table: "StudentSubscriptions",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "OutputTokensUsed",
                table: "StudentSubscriptions",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "TotalTokensUsed",
                table: "StudentSubscriptions",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.Sql(
                """
                UPDATE SubscriptionPlans
                SET DurationDays = 30
                WHERE DurationDays <= 0;

                UPDATE SubscriptionPlans
                SET TokenQuota =
                    CASE
                        WHEN TokenQuota > 0 THEN TokenQuota * CAST(1000 AS bigint)
                        ELSE CAST(1000000 AS bigint)
                    END;

                UPDATE subscriptions
                SET subscriptions.TokenQuotaAtActivation =
                    CASE
                        WHEN subscriptions.TokenQuotaAtActivation > 0
                            THEN subscriptions.TokenQuotaAtActivation * CAST(1000 AS bigint)
                        ELSE plans.TokenQuota
                    END
                FROM StudentSubscriptions AS subscriptions
                INNER JOIN SubscriptionPlans AS plans
                    ON plans.Id = subscriptions.SubscriptionPlanId;

                UPDATE payments
                SET payments.TokenQuota =
                    CASE
                        WHEN payments.TokenQuota > 0
                            THEN payments.TokenQuota * CAST(1000 AS bigint)
                        ELSE plans.TokenQuota
                    END
                FROM PaymentTransactions AS payments
                INNER JOIN SubscriptionPlans AS plans
                    ON plans.Id = payments.SubscriptionPlanId;

                UPDATE StudentSubscriptions
                SET Status = 'Expired',
                    UpdatedAtUtc = SYSUTCDATETIME()
                WHERE Status = 'Cancelled';

                ;WITH DefaultCandidate AS
                (
                    SELECT TOP (1) Id
                    FROM SubscriptionPlans
                    WHERE IsActive = 1
                      AND Price = 0
                    ORDER BY SortOrder, CreatedAtUtc, Id
                )
                UPDATE plans
                SET IsDefault =
                    CASE WHEN plans.Id = candidate.Id THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END,
                    UpdatedAtUtc = SYSUTCDATETIME()
                FROM SubscriptionPlans AS plans
                CROSS JOIN DefaultCandidate AS candidate;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPlans_IsDefault",
                table: "SubscriptionPlans",
                column: "IsDefault",
                unique: true,
                filter: "[IsDefault] = 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_SubscriptionPlans_Default",
                table: "SubscriptionPlans",
                sql: "[IsDefault] = 0 OR ([IsActive] = 1 AND [Price] = 0)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_SubscriptionPlans_DurationDays",
                table: "SubscriptionPlans",
                sql: "[DurationDays] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_SubscriptionPlans_Price",
                table: "SubscriptionPlans",
                sql: "[Price] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_SubscriptionPlans_TokenQuota",
                table: "SubscriptionPlans",
                sql: "[TokenQuota] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_StudentSubscriptions_TokenQuotaAtActivation",
                table: "StudentSubscriptions",
                sql: "[TokenQuotaAtActivation] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_StudentSubscriptions_TokenUsage",
                table: "StudentSubscriptions",
                sql: "[InputTokensUsed] >= 0 AND [OutputTokensUsed] >= 0 AND [TotalTokensUsed] >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SubscriptionPlans_IsDefault",
                table: "SubscriptionPlans");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SubscriptionPlans_Default",
                table: "SubscriptionPlans");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SubscriptionPlans_DurationDays",
                table: "SubscriptionPlans");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SubscriptionPlans_Price",
                table: "SubscriptionPlans");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SubscriptionPlans_TokenQuota",
                table: "SubscriptionPlans");

            migrationBuilder.DropCheckConstraint(
                name: "CK_StudentSubscriptions_TokenQuotaAtActivation",
                table: "StudentSubscriptions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_StudentSubscriptions_TokenUsage",
                table: "StudentSubscriptions");

            migrationBuilder.Sql(
                """
                UPDATE SubscriptionPlans
                SET TokenQuota =
                    CASE
                        WHEN TokenQuota >= 1000000 THEN 0
                        ELSE CONVERT(bigint, CEILING(TokenQuota / 1000.0))
                    END;

                UPDATE StudentSubscriptions
                SET TokenQuotaAtActivation =
                    CASE
                        WHEN TokenQuotaAtActivation >= 1000000 THEN 0
                        ELSE CONVERT(bigint, CEILING(TokenQuotaAtActivation / 1000.0))
                    END;

                UPDATE PaymentTransactions
                SET TokenQuota =
                    CASE
                        WHEN TokenQuota >= 1000000 THEN 0
                        ELSE CONVERT(bigint, CEILING(TokenQuota / 1000.0))
                    END;
                """);

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "InputTokensUsed",
                table: "StudentSubscriptions");

            migrationBuilder.DropColumn(
                name: "OutputTokensUsed",
                table: "StudentSubscriptions");

            migrationBuilder.DropColumn(
                name: "TotalTokensUsed",
                table: "StudentSubscriptions");

            migrationBuilder.AlterColumn<int>(
                name: "TokenQuota",
                table: "SubscriptionPlans",
                type: "int",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<int>(
                name: "TokenQuotaAtActivation",
                table: "StudentSubscriptions",
                type: "int",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<int>(
                name: "TokenQuota",
                table: "PaymentTransactions",
                type: "int",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.RenameColumn(
                name: "TokenQuota",
                table: "SubscriptionPlans",
                newName: "MessageQuota");

            migrationBuilder.RenameColumn(
                name: "TokenQuotaAtActivation",
                table: "StudentSubscriptions",
                newName: "MessageQuotaAtActivation");

            migrationBuilder.RenameColumn(
                name: "TokenQuota",
                table: "PaymentTransactions",
                newName: "MessageQuota");

            migrationBuilder.RenameColumn(
                name: "Price",
                table: "SubscriptionPlans",
                newName: "MonthlyPrice");

            migrationBuilder.CreateTable(
                name: "ChatMessageUsages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Count = table.Column<int>(type: "int", nullable: false),
                    PeriodKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    StudentUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMessageUsages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessageUsages_StudentUserId_PeriodKey",
                table: "ChatMessageUsages",
                columns: new[] { "StudentUserId", "PeriodKey" },
                unique: true);
        }
    }
}
