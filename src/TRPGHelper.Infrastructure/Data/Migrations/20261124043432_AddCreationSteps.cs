using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TRPGHelper.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCreationSteps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CreationSteps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SystemCode = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    Kind = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreationSteps", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CreationSteps_SystemCode_IsEnabled_SortOrder",
                table: "CreationSteps",
                columns: new[] { "SystemCode", "IsEnabled", "SortOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CreationSteps");
        }
    }
}
