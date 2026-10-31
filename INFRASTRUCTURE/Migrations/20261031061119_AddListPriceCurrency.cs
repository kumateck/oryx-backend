using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddListPriceCurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CurrencyId",
                table: "ProductPrices",
                type: "uuid",
                nullable: true);

            // Existing list prices were always read as the base currency by invoices, so record
            // that explicitly. Amounts are not touched. Without a base currency the rows stay
            // empty and are priced in the base currency once one is set. Down drops the column,
            // which removes the backfill with it.
            migrationBuilder.Sql("""
                UPDATE "ProductPrices"
                SET "CurrencyId" = (
                    SELECT c."Id" FROM "Currencies" c
                    WHERE c."IsBaseCurrency" AND c."DeletedAt" IS NULL
                    ORDER BY c."CreatedAt" LIMIT 1)
                WHERE "CurrencyId" IS NULL;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_ProductPrices_CurrencyId",
                table: "ProductPrices",
                column: "CurrencyId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductPrices_Currencies_CurrencyId",
                table: "ProductPrices",
                column: "CurrencyId",
                principalTable: "Currencies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductPrices_Currencies_CurrencyId",
                table: "ProductPrices");

            migrationBuilder.DropIndex(
                name: "IX_ProductPrices_CurrencyId",
                table: "ProductPrices");

            migrationBuilder.DropColumn(
                name: "CurrencyId",
                table: "ProductPrices");
        }
    }
}
