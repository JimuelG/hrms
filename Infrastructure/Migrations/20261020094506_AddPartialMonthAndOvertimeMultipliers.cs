using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPartialMonthAndOvertimeMultipliers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "HolidayOvertimeMuliplier",
                table: "PayrollSettings",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RestDayOvertimeMultiplier",
                table: "PayrollSettings",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "HolidayOvertimeMultiplier",
                table: "PayrollRuns",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RestDayOvertimeMultiplier",
                table: "PayrollRuns",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HolidayOvertimeMuliplier",
                table: "PayrollSettings");

            migrationBuilder.DropColumn(
                name: "RestDayOvertimeMultiplier",
                table: "PayrollSettings");

            migrationBuilder.DropColumn(
                name: "HolidayOvertimeMultiplier",
                table: "PayrollRuns");

            migrationBuilder.DropColumn(
                name: "RestDayOvertimeMultiplier",
                table: "PayrollRuns");
        }
    }
}
