using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SuperBodegaAPI.Migrations
{
    /// <inheritdoc />
    public partial class UpdateProductModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Categoria",
                table: "Products");

            migrationBuilder.RenameColumn(
                name: "Existencia",
                table: "Products",
                newName: "Stock");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Stock",
                table: "Products",
                newName: "Existencia");

            migrationBuilder.AddColumn<string>(
                name: "Categoria",
                table: "Products",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
