using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IODA.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddContentOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "order",
                table: "contents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Asignar orden único por parent_content_id (0-based) para datos existentes (columnas con comillas por PascalCase en BD)
            migrationBuilder.Sql(@"
UPDATE contents SET ""order"" = sub.rn
FROM (
    SELECT ""Id"", (ROW_NUMBER() OVER (PARTITION BY parent_content_id ORDER BY created_at, ""Id"") - 1)::integer AS rn
    FROM contents
) AS sub
WHERE contents.""Id"" = sub.""Id"";
");

            migrationBuilder.CreateIndex(
                name: "ix_contents_parent_order",
                table: "contents",
                columns: new[] { "parent_content_id", "order" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_contents_parent_order",
                table: "contents");

            migrationBuilder.DropColumn(
                name: "order",
                table: "contents");
        }
    }
}
