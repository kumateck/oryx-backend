using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class ChangePackingStyleToProductPacking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FinishedGoodsTransferNotes_PackageStyles_PackageStyleId",
                table: "FinishedGoodsTransferNotes");

            migrationBuilder.RenameColumn(
                name: "PackageStyleId",
                table: "FinishedGoodsTransferNotes",
                newName: "ProductPackingId");

            migrationBuilder.RenameIndex(
                name: "IX_FinishedGoodsTransferNotes_PackageStyleId",
                table: "FinishedGoodsTransferNotes",
                newName: "IX_FinishedGoodsTransferNotes_ProductPackingId");

            migrationBuilder.AddForeignKey(
                name: "FK_FinishedGoodsTransferNotes_ProductPackings_ProductPackingId",
                table: "FinishedGoodsTransferNotes",
                column: "ProductPackingId",
                principalTable: "ProductPackings",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FinishedGoodsTransferNotes_ProductPackings_ProductPackingId",
                table: "FinishedGoodsTransferNotes");

            migrationBuilder.RenameColumn(
                name: "ProductPackingId",
                table: "FinishedGoodsTransferNotes",
                newName: "PackageStyleId");

            migrationBuilder.RenameIndex(
                name: "IX_FinishedGoodsTransferNotes_ProductPackingId",
                table: "FinishedGoodsTransferNotes",
                newName: "IX_FinishedGoodsTransferNotes_PackageStyleId");

            migrationBuilder.AddForeignKey(
                name: "FK_FinishedGoodsTransferNotes_PackageStyles_PackageStyleId",
                table: "FinishedGoodsTransferNotes",
                column: "PackageStyleId",
                principalTable: "PackageStyles",
                principalColumn: "Id");
        }
    }
}
