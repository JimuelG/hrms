using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddThirteenthMonth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ThirteenthMonthExemptCeiling",
                table: "PayrollSettings",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Kind",
                table: "PayrollRuns",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "ThirteenthMonthExemptCeiling",
                table: "PayrollRuns",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRuns_TenantId_Kind_PeriodStart_PeriodEnd",
                table: "PayrollRuns",
                columns: new[] { "TenantId", "Kind", "PeriodStart", "PeriodEnd" },
                unique: true,
                filter: "[Status] <> 4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PayrollRuns_TenantId_Kind_PeriodStart_PeriodEnd",
                table: "PayrollRuns");

            migrationBuilder.DropColumn(
                name: "ThirteenthMonthExemptCeiling",
                table: "PayrollSettings");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "PayrollRuns");

            migrationBuilder.DropColumn(
                name: "ThirteenthMonthExemptCeiling",
                table: "PayrollRuns");
        }
    }
}
