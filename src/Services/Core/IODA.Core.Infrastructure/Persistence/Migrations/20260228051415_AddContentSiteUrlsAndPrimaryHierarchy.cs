using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IODA.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddContentSiteUrlsAndPrimaryHierarchy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_primary",
                table: "content_hierarchies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "content_site_urls",
                columns: table => new
                {
                    content_id = table.Column<Guid>(type: "uuid", nullable: false),
                    site_id = table.Column<Guid>(type: "uuid", nullable: false),
                    path = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_content_site_urls", x => new { x.content_id, x.site_id });
                    table.ForeignKey(
                        name: "FK_content_site_urls_contents_content_id",
                        column: x => x.content_id,
                        principalTable: "contents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_content_site_urls_sites_site_id",
                        column: x => x.site_id,
                        principalTable: "sites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_content_hierarchies_content_primary",
                table: "content_hierarchies",
                columns: new[] { "content_id", "is_primary" },
                unique: true,
                filter: "is_primary = true");

            migrationBuilder.CreateIndex(
                name: "ix_content_site_urls_site_id",
                table: "content_site_urls",
                column: "site_id");

            migrationBuilder.CreateIndex(
                name: "ix_content_site_urls_site_path",
                table: "content_site_urls",
                columns: new[] { "site_id", "path" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "content_site_urls");

            migrationBuilder.DropIndex(
                name: "ix_content_hierarchies_content_primary",
                table: "content_hierarchies");

            migrationBuilder.DropColumn(
                name: "is_primary",
                table: "content_hierarchies");
        }
    }
}
