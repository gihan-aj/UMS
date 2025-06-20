using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNumericSequences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "entity_sequences");

            migrationBuilder.CreateTable(
                name: "numeric_sequences",
                columns: table => new
                {
                    sequence_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    last_value = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_numeric_sequences", x => x.sequence_name);
                });

            migrationBuilder.CreateTable(
                name: "reference_code_sequences",
                columns: table => new
                {
                    entity_type_prefix = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: false),
                    sequence_date = table.Column<DateTime>(type: "date", nullable: false),
                    last_value = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reference_code_sequences", x => new { x.entity_type_prefix, x.sequence_date });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "numeric_sequences");

            migrationBuilder.DropTable(
                name: "reference_code_sequences");

            migrationBuilder.CreateTable(
                name: "entity_sequences",
                columns: table => new
                {
                    entity_type_prefix = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: false),
                    sequence_date = table.Column<DateTime>(type: "date", nullable: false),
                    last_value = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_entity_sequences", x => new { x.entity_type_prefix, x.sequence_date });
                });
        }
    }
}
