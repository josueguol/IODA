using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IODA.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSiteUrlTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "url_template",
                table: "sites",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "url_template",
                table: "sites");
        }
    }
}
