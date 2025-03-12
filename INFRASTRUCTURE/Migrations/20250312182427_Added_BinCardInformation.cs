using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class Added_BinCardInformation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BinCardInformation_MaterialBatches_MaterialBatchId",
                table: "BinCardInformation");

            migrationBuilder.RenameColumn(
                name: "MaterialBatchId",
                table: "BinCardInformation",
                newName: "BatchId");

            migrationBuilder.RenameIndex(
                name: "IX_BinCardInformation_MaterialBatchId",
                table: "BinCardInformation",
                newName: "IX_BinCardInformation_BatchId");

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "BinCardInformation",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddForeignKey(
                name: "FK_BinCardInformation_MaterialBatches_BatchId",
                table: "BinCardInformation",
                column: "BatchId",
                principalTable: "MaterialBatches",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BinCardInformation_MaterialBatches_BatchId",
                table: "BinCardInformation");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "BinCardInformation");

            migrationBuilder.RenameColumn(
                name: "BatchId",
                table: "BinCardInformation",
                newName: "MaterialBatchId");

            migrationBuilder.RenameIndex(
                name: "IX_BinCardInformation_BatchId",
                table: "BinCardInformation",
                newName: "IX_BinCardInformation_MaterialBatchId");

            migrationBuilder.AddForeignKey(
                name: "FK_BinCardInformation_MaterialBatches_MaterialBatchId",
                table: "BinCardInformation",
                column: "MaterialBatchId",
                principalTable: "MaterialBatches",
                principalColumn: "Id");
        }
    }
}
