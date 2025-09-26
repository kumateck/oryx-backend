using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class UpdateBillingSheetCharge : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Charges_Currencies_CurrencyId",
                table: "Charges");

            migrationBuilder.DropIndex(
                name: "IX_Charges_CurrencyId",
                table: "Charges");

            migrationBuilder.DropColumn(
                name: "Amount",
                table: "Charges");

            migrationBuilder.DropColumn(
                name: "CurrencyId",
                table: "Charges");

            migrationBuilder.AddColumn<decimal>(
                name: "Amount",
                table: "BillingSheetCharges",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "CurrencyId",
                table: "BillingSheetCharges",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BillingSheetCharges_CurrencyId",
                table: "BillingSheetCharges",
                column: "CurrencyId");

            migrationBuilder.AddForeignKey(
                name: "FK_BillingSheetCharges_Currencies_CurrencyId",
                table: "BillingSheetCharges",
                column: "CurrencyId",
                principalTable: "Currencies",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BillingSheetCharges_Currencies_CurrencyId",
                table: "BillingSheetCharges");

            migrationBuilder.DropIndex(
                name: "IX_BillingSheetCharges_CurrencyId",
                table: "BillingSheetCharges");

            migrationBuilder.DropColumn(
                name: "Amount",
                table: "BillingSheetCharges");

            migrationBuilder.DropColumn(
                name: "CurrencyId",
                table: "BillingSheetCharges");

            migrationBuilder.AddColumn<decimal>(
                name: "Amount",
                table: "Charges",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "CurrencyId",
                table: "Charges",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Charges_CurrencyId",
                table: "Charges",
                column: "CurrencyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Charges_Currencies_CurrencyId",
                table: "Charges",
                column: "CurrencyId",
                principalTable: "Currencies",
                principalColumn: "Id");
        }
    }
}
