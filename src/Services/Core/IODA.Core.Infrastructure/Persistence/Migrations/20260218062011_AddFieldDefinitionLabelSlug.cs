using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IODA.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFieldDefinitionLabelSlug : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "label",
                table: "field_definitions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "slug",
                table: "field_definitions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            // Backfill: label = field_name; slug = kebab-case from field_name (lowercase, non-alphanumeric to hyphen, collapse hyphens)
            migrationBuilder.Sql(@"
                UPDATE field_definitions
                SET
                    label = field_name,
                    slug = trim(both '-' from regexp_replace(lower(regexp_replace(field_name, '[^a-zA-Z0-9]+', '-', 'g')), '-+', '-', 'g'))
                WHERE label IS NULL OR slug IS NULL;
            ");

            migrationBuilder.AlterColumn<string>(
                name: "label",
                table: "field_definitions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "slug",
                table: "field_definitions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_field_definitions_schema_slug",
                table: "field_definitions",
                columns: new[] { "schema_id", "slug" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_field_definitions_schema_slug",
                table: "field_definitions");

            migrationBuilder.DropColumn(
                name: "label",
                table: "field_definitions");

            migrationBuilder.DropColumn(
                name: "slug",
                table: "field_definitions");
        }
    }
}
