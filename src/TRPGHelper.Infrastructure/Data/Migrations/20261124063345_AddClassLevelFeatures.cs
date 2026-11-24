using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TRPGHelper.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddClassLevelFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClassLevelFeatures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SystemCode = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    ClassCode = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassLevelFeatures", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClassLevelFeatures_SystemCode_ClassCode_Level_SortOrder",
                table: "ClassLevelFeatures",
                columns: new[] { "SystemCode", "ClassCode", "Level", "SortOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClassLevelFeatures");
        }
    }
}
