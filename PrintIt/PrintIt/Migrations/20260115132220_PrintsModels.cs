using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrintIt.Migrations
{
    /// <inheritdoc />
    public partial class PrintsModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Prints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MaterialType = table.Column<int>(type: "int", nullable: false),
                    Weight = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Prints", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ShopCarts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopCarts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShopCarts_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CartItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShopCartId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrintId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CartItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CartItems_Prints_PrintId",
                        column: x => x.PrintId,
                        principalTable: "Prints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CartItems_ShopCarts_ShopCartId",
                        column: x => x.ShopCartId,
                        principalTable: "ShopCarts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_PrintId",
                table: "CartItems",
                column: "PrintId");

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_ShopCartId_PrintId",
                table: "CartItems",
                columns: new[] { "ShopCartId", "PrintId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShopCarts_UserId",
                table: "ShopCarts",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CartItems");

            migrationBuilder.DropTable(
                name: "Prints");

            migrationBuilder.DropTable(
                name: "ShopCarts");
        }
    }
}
