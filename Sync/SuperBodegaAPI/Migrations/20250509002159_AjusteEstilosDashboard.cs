using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SuperBodegaAPI.Migrations
{
    /// <inheritdoc />
    public partial class AjusteEstilosDashboard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImagenUrl",
                table: "Products",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImagenUrl",
                table: "Products");
        }
    }
}
