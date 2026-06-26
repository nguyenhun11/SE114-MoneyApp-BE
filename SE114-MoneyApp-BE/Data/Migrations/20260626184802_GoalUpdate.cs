using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SE114_MoneyApp_BE.Migrations
{
    /// <inheritdoc />
    public partial class GoalUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. CHỈ ĐỊNH THÊM CỘT LockedBalance VÀO Accounts
            migrationBuilder.AddColumn<decimal>(
                name: "LockedBalance",
                table: "Accounts",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            // 2. THÊM AccountId VÀO GoalRecords
            migrationBuilder.AddColumn<Guid>(
                name: "AccountId",
                table: "GoalRecords",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_GoalRecords_AccountId",
                table: "GoalRecords",
                column: "AccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_GoalRecords_Accounts_AccountId",
                table: "GoalRecords",
                column: "AccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GoalRecords_Accounts_AccountId",
                table: "GoalRecords");

            migrationBuilder.DropIndex(
                name: "IX_GoalRecords_AccountId",
                table: "GoalRecords");

            migrationBuilder.DropColumn(
                name: "AccountId",
                table: "GoalRecords");

            // KHI DOWN THÌ CHỈ CẦN XÓA CỘT LockedBalance
            migrationBuilder.DropColumn(
                name: "LockedBalance",
                table: "Accounts");
        }
    }
}