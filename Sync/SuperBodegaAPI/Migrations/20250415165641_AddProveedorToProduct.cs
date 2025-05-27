using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SuperBodegaAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddProveedorToProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProveedorId",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Products_ProveedorId",
                table: "Products",
                column: "ProveedorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Proveedores_ProveedorId",
                table: "Products",
                column: "ProveedorId",
                principalTable: "Proveedores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_Proveedores_ProveedorId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_ProveedorId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ProveedorId",
                table: "Products");
        }
    }
}
