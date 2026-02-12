using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IODA.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSchemaInheritance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "parent_schema_id",
                table: "content_schemas",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_content_schemas_parent_schema_id",
                table: "content_schemas",
                column: "parent_schema_id");

            migrationBuilder.AddForeignKey(
                name: "FK_content_schemas_content_schemas_parent_schema_id",
                table: "content_schemas",
                column: "parent_schema_id",
                principalTable: "content_schemas",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_content_schemas_content_schemas_parent_schema_id",
                table: "content_schemas");

            migrationBuilder.DropIndex(
                name: "IX_content_schemas_parent_schema_id",
                table: "content_schemas");

            migrationBuilder.DropColumn(
                name: "parent_schema_id",
                table: "content_schemas");
        }
    }
}
