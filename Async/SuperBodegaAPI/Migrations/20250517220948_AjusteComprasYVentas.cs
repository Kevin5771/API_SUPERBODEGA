using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SuperBodegaAPI.Migrations
{
    /// <inheritdoc />
    public partial class AjusteComprasYVentas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Compras_Products_ProductoId",
                table: "Compras");

            migrationBuilder.DropForeignKey(
                name: "FK_DetalleCompras_Products_ProductoId",
                table: "DetalleCompras");

            migrationBuilder.DropForeignKey(
                name: "FK_Ventas_Products_ProductoId",
                table: "Ventas");

            migrationBuilder.DropIndex(
                name: "IX_Ventas_ProductoId",
                table: "Ventas");

            migrationBuilder.DropIndex(
                name: "IX_Compras_ProductoId",
                table: "Compras");

            migrationBuilder.DropColumn(
                name: "Cantidad",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "ProductoId",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "Cantidad",
                table: "Compras");

            migrationBuilder.DropColumn(
                name: "ProductoId",
                table: "Compras");

            migrationBuilder.RenameColumn(
                name: "PrecioUnitario",
                table: "Ventas",
                newName: "Total");

            migrationBuilder.RenameColumn(
                name: "PrecioUnitario",
                table: "Compras",
                newName: "Total");

            migrationBuilder.AddColumn<int>(
                name: "ProductId",
                table: "Compras",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Compras_ProductId",
                table: "Compras",
                column: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_Compras_Products_ProductId",
                table: "Compras",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DetalleCompras_Products_ProductoId",
                table: "DetalleCompras",
                column: "ProductoId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Compras_Products_ProductId",
                table: "Compras");

            migrationBuilder.DropForeignKey(
                name: "FK_DetalleCompras_Products_ProductoId",
                table: "DetalleCompras");

            migrationBuilder.DropIndex(
                name: "IX_Compras_ProductId",
                table: "Compras");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "Compras");

            migrationBuilder.RenameColumn(
                name: "Total",
                table: "Ventas",
                newName: "PrecioUnitario");

            migrationBuilder.RenameColumn(
                name: "Total",
                table: "Compras",
                newName: "PrecioUnitario");

            migrationBuilder.AddColumn<int>(
                name: "Cantidad",
                table: "Ventas",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ProductoId",
                table: "Ventas",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Cantidad",
                table: "Compras",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ProductoId",
                table: "Compras",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Ventas_ProductoId",
                table: "Ventas",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_Compras_ProductoId",
                table: "Compras",
                column: "ProductoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Compras_Products_ProductoId",
                table: "Compras",
                column: "ProductoId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DetalleCompras_Products_ProductoId",
                table: "DetalleCompras",
                column: "ProductoId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Ventas_Products_ProductoId",
                table: "Ventas",
                column: "ProductoId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
