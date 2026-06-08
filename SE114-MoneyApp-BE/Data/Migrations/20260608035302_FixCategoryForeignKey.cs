using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SE114_MoneyApp_BE.Migrations
{
    /// <inheritdoc />
    public partial class FixCategoryForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Categories_CategoryGroups_CategoryGroupId",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Categories_CategoryGroupId",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "CategoryGroupId",
                table: "Categories");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_GroupId",
                table: "Categories",
                column: "GroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_Categories_CategoryGroups_GroupId",
                table: "Categories",
                column: "GroupId",
                principalTable: "CategoryGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Categories_CategoryGroups_GroupId",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Categories_GroupId",
                table: "Categories");

            migrationBuilder.AddColumn<Guid>(
                name: "CategoryGroupId",
                table: "Categories",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_CategoryGroupId",
                table: "Categories",
                column: "CategoryGroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_Categories_CategoryGroups_CategoryGroupId",
                table: "Categories",
                column: "CategoryGroupId",
                principalTable: "CategoryGroups",
                principalColumn: "Id");
        }
    }
}
