using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SE114_MoneyApp_BE.Migrations
{
    /// <inheritdoc />
    public partial class AddTotalPointsToCityState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GoalRecords_Accounts_AccountId",
                table: "GoalRecords");

            migrationBuilder.AddColumn<int>(
                name: "TotalProsperityPoints",
                table: "CityStates",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalStabilityPoints",
                table: "CityStates",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddForeignKey(
                name: "FK_GoalRecords_Accounts_AccountId",
                table: "GoalRecords",
                column: "AccountId",
                principalTable: "Accounts",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GoalRecords_Accounts_AccountId",
                table: "GoalRecords");

            migrationBuilder.DropColumn(
                name: "TotalProsperityPoints",
                table: "CityStates");

            migrationBuilder.DropColumn(
                name: "TotalStabilityPoints",
                table: "CityStates");

            migrationBuilder.AddForeignKey(
                name: "FK_GoalRecords_Accounts_AccountId",
                table: "GoalRecords",
                column: "AccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
