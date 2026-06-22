using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SE114_MoneyApp_BE.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionMood : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MoodId",
                table: "Transactions",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MoodId",
                table: "Transactions");
        }
    }
}
