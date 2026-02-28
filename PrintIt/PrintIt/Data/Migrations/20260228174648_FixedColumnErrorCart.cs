using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrintIt.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixedColumnErrorCart : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CartItems_ShopCartId_PrintId",
                table: "CartItems");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_CartItems_ShopCartId_PrintId",
                table: "CartItems",
                columns: new[] { "ShopCartId", "PrintId" },
                unique: true);
        }
    }
}
