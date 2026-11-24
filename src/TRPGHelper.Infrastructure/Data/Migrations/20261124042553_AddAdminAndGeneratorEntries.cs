using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TRPGHelper.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminAndGeneratorEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "Users",
                type: "TEXT",
                maxLength: 16,
                nullable: false,
                defaultValue: "user");

            migrationBuilder.AddColumn<string>(
                name: "NameTemplate",
                table: "GameSystems",
                type: "TEXT",
                maxLength: 128,
                nullable: false,
                defaultValue: "{surname}{given}");

            migrationBuilder.CreateTable(
                name: "GeneratorEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SystemCode = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    Kind = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Value = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    ExtraJson = table.Column<string>(type: "TEXT", nullable: true),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeneratorEntries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GeneratorEntries_Kind_Category",
                table: "GeneratorEntries",
                columns: new[] { "Kind", "Category" });

            migrationBuilder.CreateIndex(
                name: "IX_GeneratorEntries_SystemCode_Kind_IsEnabled",
                table: "GeneratorEntries",
                columns: new[] { "SystemCode", "Kind", "IsEnabled" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GeneratorEntries");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "NameTemplate",
                table: "GameSystems");
        }
    }
}
