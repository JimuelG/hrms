using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTaxMethod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "RestDayOvertimeMultiplier",
                table: "PayrollSettings",
                type: "decimal(5,3)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "OrdinaryOvertimeMultiplier",
                table: "PayrollSettings",
                type: "decimal(5,3)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "HolidayOvertimeMultiplier",
                table: "PayrollSettings",
                type: "decimal(5,3)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TaxMethod",
                table: "PayrollSettings",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AlterColumn<decimal>(
                name: "RestDayOvertimeMultiplier",
                table: "PayrollRuns",
                type: "decimal(5,3)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "HolidayOvertimeMultiplier",
                table: "PayrollRuns",
                type: "decimal(5,3)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TaxMethod",
                table: "PayrollRuns",
                type: "int",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TaxMethod",
                table: "PayrollSettings");

            migrationBuilder.DropColumn(
                name: "TaxMethod",
                table: "PayrollRuns");

            migrationBuilder.AlterColumn<decimal>(
                name: "RestDayOvertimeMultiplier",
                table: "PayrollSettings",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,3)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "OrdinaryOvertimeMultiplier",
                table: "PayrollSettings",
                type: "decimal(5,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,3)");

            migrationBuilder.AlterColumn<decimal>(
                name: "HolidayOvertimeMultiplier",
                table: "PayrollSettings",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,3)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "RestDayOvertimeMultiplier",
                table: "PayrollRuns",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,3)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "HolidayOvertimeMultiplier",
                table: "PayrollRuns",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,3)",
                oldNullable: true);
        }
    }
}
