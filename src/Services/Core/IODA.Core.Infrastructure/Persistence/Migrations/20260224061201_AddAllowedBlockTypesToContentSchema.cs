using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IODA.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAllowedBlockTypesToContentSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "allowed_block_types",
                table: "content_schemas",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[]'::jsonb");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "allowed_block_types",
                table: "content_schemas");
        }
    }
}
