using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TRPGHelper.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddKeywordSummary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Summary",
                table: "Keywords",
                type: "TEXT",
                maxLength: 256,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Summary",
                table: "Keywords");
        }
    }
}
