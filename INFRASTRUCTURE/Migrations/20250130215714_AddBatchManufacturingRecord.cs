using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddBatchManufacturingRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BatchManufacturingRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchNumber = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ManufacturingDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BatchQuantity = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BatchManufacturingRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BatchManufacturingRecords_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BatchManufacturingRecords_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BatchManufacturingRecords_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BatchManufacturingRecords_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "BatchPackagingRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchNumber = table.Column<string>(type: "text", nullable: true),
                    ManufacturingDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BatchQuantity = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BatchPackagingRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BatchPackagingRecords_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BatchPackagingRecords_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BatchPackagingRecords_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BatchPackagingRecords_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_BatchManufacturingRecords_CreatedById",
                table: "BatchManufacturingRecords",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_BatchManufacturingRecords_LastDeletedById",
                table: "BatchManufacturingRecords",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_BatchManufacturingRecords_LastUpdatedById",
                table: "BatchManufacturingRecords",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_BatchManufacturingRecords_ProductId",
                table: "BatchManufacturingRecords",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_BatchPackagingRecords_CreatedById",
                table: "BatchPackagingRecords",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_BatchPackagingRecords_LastDeletedById",
                table: "BatchPackagingRecords",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_BatchPackagingRecords_LastUpdatedById",
                table: "BatchPackagingRecords",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_BatchPackagingRecords_ProductId",
                table: "BatchPackagingRecords",
                column: "ProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BatchManufacturingRecords");

            migrationBuilder.DropTable(
                name: "BatchPackagingRecords");
        }
    }
}
