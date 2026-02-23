using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrintIt.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCartItemIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Colours",
                table: "CartItems",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_ShopCartId_PrintId_Colours",
                table: "CartItems",
                columns: new[] { "ShopCartId", "PrintId", "Colours" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CartItems_ShopCartId_PrintId_Colours",
                table: "CartItems");

            migrationBuilder.AlterColumn<string>(
                name: "Colours",
                table: "CartItems",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
