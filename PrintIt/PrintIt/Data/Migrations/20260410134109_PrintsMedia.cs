using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrintIt.Data.Migrations
{
    /// <inheritdoc />
    public partial class PrintsMedia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "FilePath",
                table: "Prints",
                newName: "MediaFolderPath");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MediaFolderPath",
                table: "Prints",
                newName: "FilePath");
        }
    }
}
