using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SE114_MoneyApp_BE.Migrations
{
    /// <inheritdoc />
    public partial class AddCurrencySystem_Fixed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DefaultCurrency",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "DestinationAmount",
                table: "Transfers",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<double>(
                name: "DestinationExchangeRate",
                table: "Transfers",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<decimal>(
                name: "SourceAmount",
                table: "Transfers",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<double>(
                name: "SourceExchangeRate",
                table: "Transfers",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<decimal>(
                name: "AccountAmount",
                table: "Transactions",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "CurrencyCode",
                table: "Transactions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "ExchangeRate",
                table: "Transactions",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<decimal>(
                name: "OriginalAmount",
                table: "Transactions",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "CurrencyCode",
                table: "Accounts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultCurrency",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DestinationAmount",
                table: "Transfers");

            migrationBuilder.DropColumn(
                name: "DestinationExchangeRate",
                table: "Transfers");

            migrationBuilder.DropColumn(
                name: "SourceAmount",
                table: "Transfers");

            migrationBuilder.DropColumn(
                name: "SourceExchangeRate",
                table: "Transfers");

            migrationBuilder.DropColumn(
                name: "AccountAmount",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "CurrencyCode",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ExchangeRate",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "OriginalAmount",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "CurrencyCode",
                table: "Accounts");
        }
    }
}
