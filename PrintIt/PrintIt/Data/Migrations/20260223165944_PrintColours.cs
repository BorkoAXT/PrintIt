using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrintIt.Data.Migrations
{
    /// <inheritdoc />
    public partial class PrintColours : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Colours",
                table: "CartItems",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Colours",
                table: "CartItems");
        }
    }
}
