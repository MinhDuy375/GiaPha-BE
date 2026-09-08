using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LacVietGenealogy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMemberProfileFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BirthDay",
                table: "Members",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BirthLunarDay",
                table: "Members",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BirthLunarMonth",
                table: "Members",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BirthLunarYear",
                table: "Members",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BirthMonth",
                table: "Members",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BirthOrder",
                table: "Members",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CurrentResidence",
                table: "Members",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "DeathDay",
                table: "Members",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeathLunarDay",
                table: "Members",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeathLunarMonth",
                table: "Members",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeathLunarYear",
                table: "Members",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeathMonth",
                table: "Members",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsInLaw",
                table: "Members",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "Members",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Occupation",
                table: "Members",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "OtherNames",
                table: "Members",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "Members",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BirthDay",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "BirthLunarDay",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "BirthLunarMonth",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "BirthLunarYear",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "BirthMonth",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "BirthOrder",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "CurrentResidence",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "DeathDay",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "DeathLunarDay",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "DeathLunarMonth",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "DeathLunarYear",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "DeathMonth",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "IsInLaw",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "Note",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "Occupation",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "OtherNames",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "Members");

        }
    }
}
