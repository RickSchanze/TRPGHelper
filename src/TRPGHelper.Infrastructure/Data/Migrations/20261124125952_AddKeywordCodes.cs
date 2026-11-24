using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TRPGHelper.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddKeywordCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Keywords",
                type: "TEXT",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "KeywordCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 8, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KeywordCategories", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KeywordCategories_Code",
                table: "KeywordCategories",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KeywordCategories_Name",
                table: "KeywordCategories",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KeywordCategories");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Keywords");
        }
    }
}
