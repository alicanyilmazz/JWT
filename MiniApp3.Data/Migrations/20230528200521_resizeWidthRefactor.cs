using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniApp3.Data.Migrations
{
    public partial class resizeWidthRefactor : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsOriginal",
                table: "ImageQuality",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsOriginal",
                table: "ImageQuality");
        }
    }
}
