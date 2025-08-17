using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ChangeClientRedirectUriTableName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_client_redirect_uri_clients_client_id",
                table: "client_redirect_uri");

            migrationBuilder.DropPrimaryKey(
                name: "pk_client_redirect_uri",
                table: "client_redirect_uri");

            migrationBuilder.RenameTable(
                name: "client_redirect_uri",
                newName: "client_redirect_uris");

            migrationBuilder.RenameIndex(
                name: "ix_client_redirect_uri_client_id_uri",
                table: "client_redirect_uris",
                newName: "ix_client_redirect_uris_client_id_uri");

            migrationBuilder.AddPrimaryKey(
                name: "pk_client_redirect_uris",
                table: "client_redirect_uris",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_client_redirect_uris_clients_client_id",
                table: "client_redirect_uris",
                column: "client_id",
                principalTable: "clients",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_client_redirect_uris_clients_client_id",
                table: "client_redirect_uris");

            migrationBuilder.DropPrimaryKey(
                name: "pk_client_redirect_uris",
                table: "client_redirect_uris");

            migrationBuilder.RenameTable(
                name: "client_redirect_uris",
                newName: "client_redirect_uri");

            migrationBuilder.RenameIndex(
                name: "ix_client_redirect_uris_client_id_uri",
                table: "client_redirect_uri",
                newName: "ix_client_redirect_uri_client_id_uri");

            migrationBuilder.AddPrimaryKey(
                name: "pk_client_redirect_uri",
                table: "client_redirect_uri",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_client_redirect_uri_clients_client_id",
                table: "client_redirect_uri",
                column: "client_id",
                principalTable: "clients",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
