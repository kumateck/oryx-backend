using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CurrencyId",
                table: "Suppliers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Symbol",
                table: "Currencies",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_CurrencyId",
                table: "Suppliers",
                column: "CurrencyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Suppliers_Currencies_CurrencyId",
                table: "Suppliers",
                column: "CurrencyId",
                principalTable: "Currencies",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Suppliers_Currencies_CurrencyId",
                table: "Suppliers");

            migrationBuilder.DropIndex(
                name: "IX_Suppliers_CurrencyId",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "CurrencyId",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "Symbol",
                table: "Currencies");
        }
    }
}
