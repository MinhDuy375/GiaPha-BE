using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LacVietGenealogy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEventToGalleryImagesEf : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "EventId",
                table: "GalleryImages",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.CreateIndex(
                name: "IX_GalleryImages_EventId",
                table: "GalleryImages",
                column: "EventId");

            migrationBuilder.AddForeignKey(
                name: "FK_GalleryImages_FamilyEvents_EventId",
                table: "GalleryImages",
                column: "EventId",
                principalTable: "FamilyEvents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GalleryImages_FamilyEvents_EventId",
                table: "GalleryImages");

            migrationBuilder.DropIndex(
                name: "IX_GalleryImages_EventId",
                table: "GalleryImages");

            migrationBuilder.DropColumn(
                name: "EventId",
                table: "GalleryImages");
        }
    }
}
