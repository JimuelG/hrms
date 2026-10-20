using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendanceDeductionSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "HolidayOvertimeMuliplier",
                table: "PayrollSettings",
                newName: "HolidayOvertimeMultiplier");

            migrationBuilder.RenameColumn(
                name: "WorkingDaysFactorySixDay",
                table: "PayrollRuns",
                newName: "WorkingDaysFactorSixDay");

            migrationBuilder.RenameColumn(
                name: "ScheduleEnd",
                table: "AttendanceRecords",
                newName: "ScheduledEnd");

            migrationBuilder.AddColumn<bool>(
                name: "AttendanceDeductionsEnabled",
                table: "PayrollSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "AttendanceGraceMinutes",
                table: "PayrollSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "AttendanceDeductionsEnabled",
                table: "PayrollRuns",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "AttendanceGraceMinutes",
                table: "PayrollRuns",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttendanceDeductionsEnabled",
                table: "PayrollSettings");

            migrationBuilder.DropColumn(
                name: "AttendanceGraceMinutes",
                table: "PayrollSettings");

            migrationBuilder.DropColumn(
                name: "AttendanceDeductionsEnabled",
                table: "PayrollRuns");

            migrationBuilder.DropColumn(
                name: "AttendanceGraceMinutes",
                table: "PayrollRuns");

            migrationBuilder.RenameColumn(
                name: "HolidayOvertimeMultiplier",
                table: "PayrollSettings",
                newName: "HolidayOvertimeMuliplier");

            migrationBuilder.RenameColumn(
                name: "WorkingDaysFactorSixDay",
                table: "PayrollRuns",
                newName: "WorkingDaysFactorySixDay");

            migrationBuilder.RenameColumn(
                name: "ScheduledEnd",
                table: "AttendanceRecords",
                newName: "ScheduleEnd");
        }
    }
}
