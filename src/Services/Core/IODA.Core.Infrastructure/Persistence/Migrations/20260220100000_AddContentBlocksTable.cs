using IODA.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IODA.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(CoreDbContext))]
    [Migration("20260220100000_AddContentBlocksTable")]
    public partial class AddContentBlocksTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "content_blocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    content_id = table.Column<Guid>(type: "uuid", nullable: false),
                    block_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    order = table.Column<int>(type: "integer", nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_content_blocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_content_blocks_contents_content_id",
                        column: x => x.content_id,
                        principalTable: "contents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_content_blocks_content_id_order",
                table: "content_blocks",
                columns: new[] { "content_id", "order" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "content_blocks");
        }
    }
}
