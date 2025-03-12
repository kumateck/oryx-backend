using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class Added_ProductBinCardInformation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

            migrationBuilder.CreateTable(
                name: "ProductBinCardInformation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchId = table.Column<Guid>(type: "uuid", nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    WayBill = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ArNumber = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    QuantityReceived = table.Column<decimal>(type: "numeric", nullable: false),
                    QuantityIssued = table.Column<decimal>(type: "numeric", nullable: false),
                    BalanceQuantity = table.Column<decimal>(type: "numeric", nullable: false),
                    UoMId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductBinCardInformation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductBinCardInformation_Products_BatchId",
                        column: x => x.BatchId,
                        principalTable: "Products",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductBinCardInformation_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductBinCardInformation_UnitOfMeasures_UoMId",
                        column: x => x.UoMId,
                        principalTable: "UnitOfMeasures",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductBinCardInformation_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductBinCardInformation_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductBinCardInformation_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductBinCardInformation_BatchId",
                table: "ProductBinCardInformation",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductBinCardInformation_CreatedById",
                table: "ProductBinCardInformation",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductBinCardInformation_LastDeletedById",
                table: "ProductBinCardInformation",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductBinCardInformation_LastUpdatedById",
                table: "ProductBinCardInformation",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductBinCardInformation_ProductId",
                table: "ProductBinCardInformation",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductBinCardInformation_UoMId",
                table: "ProductBinCardInformation",
                column: "UoMId");

            migrationBuilder.AddForeignKey(
                name: "FK_BinCardInformation_MaterialBatches_MaterialBatchId",
                table: "BinCardInformation",
                column: "MaterialBatchId",
                principalTable: "MaterialBatches",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BinCardInformation_MaterialBatches_MaterialBatchId",
                table: "BinCardInformation");

            migrationBuilder.DropTable(
                name: "ProductBinCardInformation");

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
    }
}
