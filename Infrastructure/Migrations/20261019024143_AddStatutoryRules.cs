using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStatutoryRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StatutoryRuleSets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Method = table.Column<int>(type: "int", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    Basis = table.Column<int>(type: "int", nullable: true),
                    EmployeeRate = table.Column<decimal>(type: "decimal(9,6)", nullable: false),
                    EmployerRate = table.Column<decimal>(type: "decimal(9,6)", nullable: false),
                    BaseFloor = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    BaseCeiling = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Source = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatutoryRuleSets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StatutoryBrackets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StatutoryRuleSetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    FromAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ToAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    EmployeeFixed = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EmployerFixed = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EmployeeRateOnExcess = table.Column<decimal>(type: "decimal(9,6)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatutoryBrackets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StatutoryBrackets_StatutoryRuleSets_StatutoryRuleSetId",
                        column: x => x.StatutoryRuleSetId,
                        principalTable: "StatutoryRuleSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StatutoryBrackets_StatutoryRuleSetId_Order",
                table: "StatutoryBrackets",
                columns: new[] { "StatutoryRuleSetId", "Order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StatutoryBrackets_TenantId",
                table: "StatutoryBrackets",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_StatutoryRuleSets_TenantId",
                table: "StatutoryRuleSets",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_StatutoryRuleSets_TenantId_Kind_Basis_EffectiveFrom",
                table: "StatutoryRuleSets",
                columns: new[] { "TenantId", "Kind", "Basis", "EffectiveFrom" },
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StatutoryBrackets");

            migrationBuilder.DropTable(
                name: "StatutoryRuleSets");
        }
    }
}
