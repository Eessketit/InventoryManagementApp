using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace InventoryApp.Migrations
{
    /// <inheritdoc />
    public partial class AddSearchVector : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_inventories_fts",
                table: "inventories");

            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "SearchVector",
                table: "inventories",
                type: "tsvector",
                nullable: false)
                .Annotation("Npgsql:TsVectorConfig", "english")
                .Annotation("Npgsql:TsVectorProperties", new[] { "Title", "Description" });

            migrationBuilder.CreateIndex(
                name: "IX_inventories_SearchVector",
                table: "inventories",
                column: "SearchVector")
                .Annotation("Npgsql:IndexMethod", "GIN");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_inventories_SearchVector",
                table: "inventories");

            migrationBuilder.DropColumn(
                name: "SearchVector",
                table: "inventories");

            migrationBuilder.CreateIndex(
                name: "IX_inventories_fts",
                table: "inventories",
                columns: new[] { "Title", "Description" });
        }
    }
}
