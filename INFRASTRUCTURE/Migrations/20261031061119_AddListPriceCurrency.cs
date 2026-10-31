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

            // Complete this table's DDL before writing rows. Report history installs a deferred
            // capture trigger on ProductPrices; a backfill before CREATE INDEX/ADD CONSTRAINT
            // leaves pending trigger events and PostgreSQL refuses that DDL in this transaction.
            // Existing invoices read these amounts as the base currency. Keep the amounts and
            // record that currency; without an active base currency the new values remain null.
            migrationBuilder.Sql("""
                UPDATE "ProductPrices"
                SET "CurrencyId" = (
                    SELECT c."Id" FROM "Currencies" c
                    WHERE c."IsBaseCurrency" AND c."DeletedAt" IS NULL
                    ORDER BY c."CreatedAt" LIMIT 1)
                WHERE "CurrencyId" IS NULL;
                """);
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
