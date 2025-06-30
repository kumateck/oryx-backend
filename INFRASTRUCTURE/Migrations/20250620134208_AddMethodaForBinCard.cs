using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddMethodaForBinCard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductBinCardInformation_Products_BatchId",
                table: "ProductBinCardInformation");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductBinCardInformation_BatchManufacturingRecords_BatchId",
                table: "ProductBinCardInformation",
                column: "BatchId",
                principalTable: "BatchManufacturingRecords",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductBinCardInformation_BatchManufacturingRecords_BatchId",
                table: "ProductBinCardInformation");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductBinCardInformation_Products_BatchId",
                table: "ProductBinCardInformation",
                column: "BatchId",
                principalTable: "Products",
                principalColumn: "Id");
        }
    }
}
