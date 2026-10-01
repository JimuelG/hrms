using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserTenantRoles_Roles_RoleId",
                table: "UserTenantRoles");

            migrationBuilder.AddForeignKey(
                name: "FK_UserTenantRoles_Roles_RoleId",
                table: "UserTenantRoles",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserTenantRoles_Roles_RoleId",
                table: "UserTenantRoles");

            migrationBuilder.AddForeignKey(
                name: "FK_UserTenantRoles_Roles_RoleId",
                table: "UserTenantRoles",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
