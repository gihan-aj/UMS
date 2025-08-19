using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace UMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddClientIdToPermissionsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_permissions_name",
                table: "permissions");

            migrationBuilder.AlterColumn<short>(
                name: "id",
                table: "permissions",
                type: "smallint",
                nullable: false,
                oldClrType: typeof(short),
                oldType: "smallint")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<Guid>(
                name: "client_id",
                table: "permissions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_permissions_client_id_name",
                table: "permissions",
                columns: new[] { "client_id", "name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_permissions__clients_client_id",
                table: "permissions",
                column: "client_id",
                principalTable: "clients",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_permissions__clients_client_id",
                table: "permissions");

            migrationBuilder.DropIndex(
                name: "ix_permissions_client_id_name",
                table: "permissions");

            migrationBuilder.DropColumn(
                name: "client_id",
                table: "permissions");

            migrationBuilder.AlterColumn<short>(
                name: "id",
                table: "permissions",
                type: "smallint",
                nullable: false,
                oldClrType: typeof(short),
                oldType: "smallint")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.CreateIndex(
                name: "ix_permissions_name",
                table: "permissions",
                column: "name",
                unique: true);
        }
    }
}
