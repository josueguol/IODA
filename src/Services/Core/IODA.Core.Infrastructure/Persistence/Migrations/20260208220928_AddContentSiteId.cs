using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IODA.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddContentSiteId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "site_id",
                table: "contents",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_contents_project_site",
                table: "contents",
                columns: new[] { "project_id", "site_id" });

            migrationBuilder.CreateIndex(
                name: "IX_contents_site_id",
                table: "contents",
                column: "site_id");

            migrationBuilder.AddForeignKey(
                name: "FK_contents_sites_site_id",
                table: "contents",
                column: "site_id",
                principalTable: "sites",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_contents_sites_site_id",
                table: "contents");

            migrationBuilder.DropIndex(
                name: "ix_contents_project_site",
                table: "contents");

            migrationBuilder.DropIndex(
                name: "IX_contents_site_id",
                table: "contents");

            migrationBuilder.DropColumn(
                name: "site_id",
                table: "contents");
        }
    }
}
