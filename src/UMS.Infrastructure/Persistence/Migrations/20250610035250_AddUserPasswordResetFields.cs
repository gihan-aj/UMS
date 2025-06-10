using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserPasswordResetFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "password_reset_token",
                table: "users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "password_reset_token_expiry_utc",
                table: "users",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "password_reset_token",
                table: "users");

            migrationBuilder.DropColumn(
                name: "password_reset_token_expiry_utc",
                table: "users");
        }
    }
}
