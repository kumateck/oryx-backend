using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceCustomerQuotationPricingUoMWithProductPacking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CustomerPricingAgreements_UnitOfMeasures_UoMId",
                table: "CustomerPricingAgreements");

            migrationBuilder.DropForeignKey(
                name: "FK_CustomerQuotationItems_UnitOfMeasures_UoMId",
                table: "CustomerQuotationItems");

            migrationBuilder.RenameColumn(
                name: "UoMId",
                table: "CustomerQuotationItems",
                newName: "ProductPackingId");

            migrationBuilder.RenameIndex(
                name: "IX_CustomerQuotationItems_UoMId",
                table: "CustomerQuotationItems",
                newName: "IX_CustomerQuotationItems_ProductPackingId");

            migrationBuilder.RenameColumn(
                name: "UoMId",
                table: "CustomerPricingAgreements",
                newName: "ProductPackingId");

            migrationBuilder.RenameIndex(
                name: "IX_CustomerPricingAgreements_UoMId",
                table: "CustomerPricingAgreements",
                newName: "IX_CustomerPricingAgreements_ProductPackingId");

            migrationBuilder.RenameIndex(
                name: "IX_CustomerPricingAgreements_CustomerId_ProductId_UoMId_Effect~",
                table: "CustomerPricingAgreements",
                newName: "IX_CustomerPricingAgreements_CustomerId_ProductId_ProductPacki~");

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerPricingAgreements_ProductPackings_ProductPackingId",
                table: "CustomerPricingAgreements",
                column: "ProductPackingId",
                principalTable: "ProductPackings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerQuotationItems_ProductPackings_ProductPackingId",
                table: "CustomerQuotationItems",
                column: "ProductPackingId",
                principalTable: "ProductPackings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CustomerPricingAgreements_ProductPackings_ProductPackingId",
                table: "CustomerPricingAgreements");

            migrationBuilder.DropForeignKey(
                name: "FK_CustomerQuotationItems_ProductPackings_ProductPackingId",
                table: "CustomerQuotationItems");

            migrationBuilder.RenameColumn(
                name: "ProductPackingId",
                table: "CustomerQuotationItems",
                newName: "UoMId");

            migrationBuilder.RenameIndex(
                name: "IX_CustomerQuotationItems_ProductPackingId",
                table: "CustomerQuotationItems",
                newName: "IX_CustomerQuotationItems_UoMId");

            migrationBuilder.RenameColumn(
                name: "ProductPackingId",
                table: "CustomerPricingAgreements",
                newName: "UoMId");

            migrationBuilder.RenameIndex(
                name: "IX_CustomerPricingAgreements_ProductPackingId",
                table: "CustomerPricingAgreements",
                newName: "IX_CustomerPricingAgreements_UoMId");

            migrationBuilder.RenameIndex(
                name: "IX_CustomerPricingAgreements_CustomerId_ProductId_ProductPacki~",
                table: "CustomerPricingAgreements",
                newName: "IX_CustomerPricingAgreements_CustomerId_ProductId_UoMId_Effect~");

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerPricingAgreements_UnitOfMeasures_UoMId",
                table: "CustomerPricingAgreements",
                column: "UoMId",
                principalTable: "UnitOfMeasures",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerQuotationItems_UnitOfMeasures_UoMId",
                table: "CustomerQuotationItems",
                column: "UoMId",
                principalTable: "UnitOfMeasures",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
