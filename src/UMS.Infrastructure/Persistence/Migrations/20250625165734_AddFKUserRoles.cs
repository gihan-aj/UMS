using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFKUserRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_user_roles_role_id",
                table: "user_roles",
                column: "role_id");

            migrationBuilder.AddForeignKey(
                name: "fk_user_roles_roles_role_id",
                table: "user_roles",
                column: "role_id",
                principalTable: "roles",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_user_roles_roles_role_id",
                table: "user_roles");

            migrationBuilder.DropIndex(
                name: "ix_user_roles_role_id",
                table: "user_roles");
        }
    }
}
