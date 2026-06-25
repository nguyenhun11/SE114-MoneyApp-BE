using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SE114_MoneyApp_BE.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryGroupId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CategoryGroupId",
                table: "Budgets",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Budgets_CategoryGroupId",
                table: "Budgets",
                column: "CategoryGroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_Budgets_CategoryGroups_CategoryGroupId",
                table: "Budgets",
                column: "CategoryGroupId",
                principalTable: "CategoryGroups",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Budgets_CategoryGroups_CategoryGroupId",
                table: "Budgets");

            migrationBuilder.DropIndex(
                name: "IX_Budgets_CategoryGroupId",
                table: "Budgets");

            migrationBuilder.DropColumn(
                name: "CategoryGroupId",
                table: "Budgets");
        }
    }
}
