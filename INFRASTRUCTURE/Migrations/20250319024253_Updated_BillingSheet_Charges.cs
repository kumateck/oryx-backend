using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class Updated_BillingSheet_Charges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Amount",
                table: "Charges",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "BillingSheetId",
                table: "Charges",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CurrencyId",
                table: "Charges",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Charges",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Charges_BillingSheetId",
                table: "Charges",
                column: "BillingSheetId");

            migrationBuilder.CreateIndex(
                name: "IX_Charges_CurrencyId",
                table: "Charges",
                column: "CurrencyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Charges_BillingSheets_BillingSheetId",
                table: "Charges",
                column: "BillingSheetId",
                principalTable: "BillingSheets",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Charges_Currencies_CurrencyId",
                table: "Charges",
                column: "CurrencyId",
                principalTable: "Currencies",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Charges_BillingSheets_BillingSheetId",
                table: "Charges");

            migrationBuilder.DropForeignKey(
                name: "FK_Charges_Currencies_CurrencyId",
                table: "Charges");

            migrationBuilder.DropIndex(
                name: "IX_Charges_BillingSheetId",
                table: "Charges");

            migrationBuilder.DropIndex(
                name: "IX_Charges_CurrencyId",
                table: "Charges");

            migrationBuilder.DropColumn(
                name: "Amount",
                table: "Charges");

            migrationBuilder.DropColumn(
                name: "BillingSheetId",
                table: "Charges");

            migrationBuilder.DropColumn(
                name: "CurrencyId",
                table: "Charges");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Charges");
        }
    }
}
