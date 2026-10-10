using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "Employees",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AttendanceRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    ClockInUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClockInWorkLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ClockInLocationName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    ClockInDistanceMeters = table.Column<double>(type: "float", nullable: true),
                    ClockInAccuracyMeters = table.Column<double>(type: "float", nullable: true),
                    ClockInLatitude = table.Column<double>(type: "float", nullable: true),
                    ClockInLongitude = table.Column<double>(type: "float", nullable: true),
                    ClockOutUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClockOutWorkLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ClockOutLocationName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    ClockOutDistanceMeters = table.Column<double>(type: "float", nullable: true),
                    ClockOutAccuracyMeters = table.Column<double>(type: "float", nullable: true),
                    ClockOutLatitude = table.Column<double>(type: "float", nullable: true),
                    ClockOutLongitude = table.Column<double>(type: "float", nullable: true),
                    ScheduledStart = table.Column<TimeOnly>(type: "time", nullable: true),
                    ScheduleEnd = table.Column<TimeOnly>(type: "time", nullable: true),
                    Flags = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttendanceRecords_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AttendanceRecords_WorkLocations_ClockInWorkLocationId",
                        column: x => x.ClockInWorkLocationId,
                        principalTable: "WorkLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AttendanceRecords_WorkLocations_ClockOutWorkLocationId",
                        column: x => x.ClockOutWorkLocationId,
                        principalTable: "WorkLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Employees_TenantId_UserId",
                table: "Employees",
                columns: new[] { "TenantId", "UserId" },
                unique: true,
                filter: "[UserId] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_ClockInWorkLocationId",
                table: "AttendanceRecords",
                column: "ClockInWorkLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_ClockOutWorkLocationId",
                table: "AttendanceRecords",
                column: "ClockOutWorkLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_Date",
                table: "AttendanceRecords",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_EmployeeId_Date",
                table: "AttendanceRecords",
                columns: new[] { "EmployeeId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_TenantId",
                table: "AttendanceRecords",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttendanceRecords");

            migrationBuilder.DropIndex(
                name: "IX_Employees_TenantId_UserId",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Employees");
        }
    }
}
