using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IODA.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHierarchiesModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "hierarchies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    image_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    parent_hierarchy_id = table.Column<Guid>(type: "uuid", nullable: true),
                    HierarchyId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hierarchies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_hierarchies_hierarchies_HierarchyId",
                        column: x => x.HierarchyId,
                        principalTable: "hierarchies",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_hierarchies_hierarchies_parent_hierarchy_id",
                        column: x => x.parent_hierarchy_id,
                        principalTable: "hierarchies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_hierarchies_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "content_hierarchies",
                columns: table => new
                {
                    content_id = table.Column<Guid>(type: "uuid", nullable: false),
                    hierarchy_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_content_hierarchies", x => new { x.content_id, x.hierarchy_id });
                    table.ForeignKey(
                        name: "FK_content_hierarchies_contents_content_id",
                        column: x => x.content_id,
                        principalTable: "contents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_content_hierarchies_hierarchies_hierarchy_id",
                        column: x => x.hierarchy_id,
                        principalTable: "hierarchies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_content_hierarchies_hierarchy_id",
                table: "content_hierarchies",
                column: "hierarchy_id");

            migrationBuilder.CreateIndex(
                name: "IX_hierarchies_HierarchyId",
                table: "hierarchies",
                column: "HierarchyId");

            migrationBuilder.CreateIndex(
                name: "ix_hierarchies_parent_id",
                table: "hierarchies",
                column: "parent_hierarchy_id");

            migrationBuilder.CreateIndex(
                name: "ix_hierarchies_project_slug",
                table: "hierarchies",
                columns: new[] { "project_id", "slug" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "content_hierarchies");

            migrationBuilder.DropTable(
                name: "hierarchies");
        }
    }
}
