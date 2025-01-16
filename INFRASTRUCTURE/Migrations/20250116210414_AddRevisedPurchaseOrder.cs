using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddRevisedPurchaseOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RevisedPurchaseOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpectedDeliveryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeliveryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RevisedPurchaseOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RevisedPurchaseOrders_PurchaseOrders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalTable: "PurchaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RevisedPurchaseOrders_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RevisedPurchaseOrders_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RevisedPurchaseOrders_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RevisedPurchaseOrderItem",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RevisedPurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    UoMId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    Price = table.Column<decimal>(type: "numeric", nullable: false),
                    CurrencyId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RevisedPurchaseOrderItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RevisedPurchaseOrderItem_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RevisedPurchaseOrderItem_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RevisedPurchaseOrderItem_RevisedPurchaseOrders_RevisedPurch~",
                        column: x => x.RevisedPurchaseOrderId,
                        principalTable: "RevisedPurchaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RevisedPurchaseOrderItem_UnitOfMeasures_UoMId",
                        column: x => x.UoMId,
                        principalTable: "UnitOfMeasures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RevisedPurchaseOrderItem_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RevisedPurchaseOrderItem_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RevisedPurchaseOrderItem_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_RevisedPurchaseOrderItem_CreatedById",
                table: "RevisedPurchaseOrderItem",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RevisedPurchaseOrderItem_CurrencyId",
                table: "RevisedPurchaseOrderItem",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_RevisedPurchaseOrderItem_LastDeletedById",
                table: "RevisedPurchaseOrderItem",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_RevisedPurchaseOrderItem_LastUpdatedById",
                table: "RevisedPurchaseOrderItem",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RevisedPurchaseOrderItem_MaterialId",
                table: "RevisedPurchaseOrderItem",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_RevisedPurchaseOrderItem_RevisedPurchaseOrderId",
                table: "RevisedPurchaseOrderItem",
                column: "RevisedPurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_RevisedPurchaseOrderItem_UoMId",
                table: "RevisedPurchaseOrderItem",
                column: "UoMId");

            migrationBuilder.CreateIndex(
                name: "IX_RevisedPurchaseOrders_CreatedById",
                table: "RevisedPurchaseOrders",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RevisedPurchaseOrders_LastDeletedById",
                table: "RevisedPurchaseOrders",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_RevisedPurchaseOrders_LastUpdatedById",
                table: "RevisedPurchaseOrders",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RevisedPurchaseOrders_PurchaseOrderId",
                table: "RevisedPurchaseOrders",
                column: "PurchaseOrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RevisedPurchaseOrderItem");

            migrationBuilder.DropTable(
                name: "RevisedPurchaseOrders");
        }
    }
}
