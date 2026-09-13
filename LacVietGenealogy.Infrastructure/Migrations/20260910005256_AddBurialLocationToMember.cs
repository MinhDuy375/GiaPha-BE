using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LacVietGenealogy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBurialLocationToMember : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BurialLocation",
                table: "Members",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BurialLocation",
                table: "Members");
        }
    }
}
