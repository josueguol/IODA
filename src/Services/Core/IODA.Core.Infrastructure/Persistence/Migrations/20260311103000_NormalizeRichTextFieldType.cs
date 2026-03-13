using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace IODA.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(CoreDbContext))]
    [Migration("20260311103000_NormalizeRichTextFieldType")]
    public partial class NormalizeRichTextFieldType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE field_definitions
                SET field_type = 'richtexteditor'
                WHERE lower(trim(field_type)) IN ('blocknote_markdown', 'markdown', 'richtext');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE field_definitions
                SET field_type = 'blocknote_markdown'
                WHERE lower(trim(field_type)) = 'richtexteditor';
                """);
        }
    }
}
