using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class Updated_ShipmentInvoice_TotalCost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CurrencyId",
                table: "ShipmentInvoices",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalCost",
                table: "ShipmentInvoices",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "CurrencyId",
                table: "ShipmentInvoiceItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalCost",
                table: "ShipmentInvoiceItems",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentInvoices_CurrencyId",
                table: "ShipmentInvoices",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentInvoiceItems_CurrencyId",
                table: "ShipmentInvoiceItems",
                column: "CurrencyId");

            migrationBuilder.AddForeignKey(
                name: "FK_ShipmentInvoiceItems_Currencies_CurrencyId",
                table: "ShipmentInvoiceItems",
                column: "CurrencyId",
                principalTable: "Currencies",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ShipmentInvoices_Currencies_CurrencyId",
                table: "ShipmentInvoices",
                column: "CurrencyId",
                principalTable: "Currencies",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ShipmentInvoiceItems_Currencies_CurrencyId",
                table: "ShipmentInvoiceItems");

            migrationBuilder.DropForeignKey(
                name: "FK_ShipmentInvoices_Currencies_CurrencyId",
                table: "ShipmentInvoices");

            migrationBuilder.DropIndex(
                name: "IX_ShipmentInvoices_CurrencyId",
                table: "ShipmentInvoices");

            migrationBuilder.DropIndex(
                name: "IX_ShipmentInvoiceItems_CurrencyId",
                table: "ShipmentInvoiceItems");

            migrationBuilder.DropColumn(
                name: "CurrencyId",
                table: "ShipmentInvoices");

            migrationBuilder.DropColumn(
                name: "TotalCost",
                table: "ShipmentInvoices");

            migrationBuilder.DropColumn(
                name: "CurrencyId",
                table: "ShipmentInvoiceItems");

            migrationBuilder.DropColumn(
                name: "TotalCost",
                table: "ShipmentInvoiceItems");
        }
    }
}
