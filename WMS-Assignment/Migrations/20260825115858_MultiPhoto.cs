using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS_Assignment.Migrations
{
    /// <inheritdoc />
    public partial class MultiPhoto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhotoURL",
                table: "MenuItems");

            migrationBuilder.RenameColumn(
                name: "Photo",
                table: "MenuItemPhotos",
                newName: "PhotoURL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PhotoURL",
                table: "MenuItemPhotos",
                newName: "Photo");

            migrationBuilder.AddColumn<string>(
                name: "PhotoURL",
                table: "MenuItems",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
