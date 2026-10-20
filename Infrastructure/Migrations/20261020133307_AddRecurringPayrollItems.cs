using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRecurringPayrollItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Tax",
                table: "PayrollAdjustments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "RecurringPayrollItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Tax = table.Column<int>(type: "int", nullable: false),
                    From = table.Column<DateOnly>(type: "date", nullable: false),
                    To = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    TotalOwed = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PaidSoFar = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecurringPayrollItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecurringPayrollItems_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LoanRepayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecurringPayrollItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PayrollRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoanRepayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoanRepayments_PayrollRuns_PayrollRunId",
                        column: x => x.PayrollRunId,
                        principalTable: "PayrollRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LoanRepayments_RecurringPayrollItems_RecurringPayrollItemId",
                        column: x => x.RecurringPayrollItemId,
                        principalTable: "RecurringPayrollItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PayrollLoanApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PayrollRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecurringPayrollItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollLoanApplications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollLoanApplications_PayrollRuns_PayrollRunId",
                        column: x => x.PayrollRunId,
                        principalTable: "PayrollRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PayrollLoanApplications_RecurringPayrollItems_RecurringPayrollItemId",
                        column: x => x.RecurringPayrollItemId,
                        principalTable: "RecurringPayrollItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LoanRepayments_PayrollRunId_RecurringPayrollItemId",
                table: "LoanRepayments",
                columns: new[] { "PayrollRunId", "RecurringPayrollItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoanRepayments_RecurringPayrollItemId",
                table: "LoanRepayments",
                column: "RecurringPayrollItemId");

            migrationBuilder.CreateIndex(
                name: "IX_LoanRepayments_TenantId",
                table: "LoanRepayments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollLoanApplications_PayrollRunId_RecurringPayrollItemId",
                table: "PayrollLoanApplications",
                columns: new[] { "PayrollRunId", "RecurringPayrollItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayrollLoanApplications_RecurringPayrollItemId",
                table: "PayrollLoanApplications",
                column: "RecurringPayrollItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollLoanApplications_TenantId",
                table: "PayrollLoanApplications",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringPayrollItems_EmployeeId_IsActive",
                table: "RecurringPayrollItems",
                columns: new[] { "EmployeeId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_RecurringPayrollItems_TenantId",
                table: "RecurringPayrollItems",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LoanRepayments");

            migrationBuilder.DropTable(
                name: "PayrollLoanApplications");

            migrationBuilder.DropTable(
                name: "RecurringPayrollItems");

            migrationBuilder.DropColumn(
                name: "Tax",
                table: "PayrollAdjustments");
        }
    }
}
