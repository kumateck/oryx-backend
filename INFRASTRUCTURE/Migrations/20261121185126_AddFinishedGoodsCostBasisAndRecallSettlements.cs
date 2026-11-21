using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddFinishedGoodsCostBasisAndRecallSettlements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CostingConfirmedAt",
                table: "AllocateProductionOrders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CostingConfirmedById",
                table: "AllocateProductionOrders",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FinishedGoodsCostingLine",
                columns: table => new
                {
                    AllocateProductionOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductPackingId = table.Column<Guid>(type: "uuid", nullable: false),
                    ManufacturingCostPerPack = table.Column<decimal>(type: "numeric", nullable: false),
                    CurrencyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovedCostBasisReference = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinishedGoodsCostingLine", x => new { x.AllocateProductionOrderId, x.Id });
                    table.ForeignKey(
                        name: "FK_FinishedGoodsCostingLine_AllocateProductionOrders_AllocateP~",
                        column: x => x.AllocateProductionOrderId,
                        principalTable: "AllocateProductionOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FinishedGoodsRecallSettlements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FinishedGoodsRecallId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "text", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    CurrencyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Evidence = table.Column<string>(type: "text", nullable: true),
                    ReplacementProductionOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinishedGoodsRecallSettlements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinishedGoodsRecallSettlements_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FinishedGoodsRecallSettlements_FinishedGoodsRecalls_Finishe~",
                        column: x => x.FinishedGoodsRecallId,
                        principalTable: "FinishedGoodsRecalls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FinishedGoodsRecallSettlements_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FinishedGoodsRecallSettlements_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FinishedGoodsRecallSettlements_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FinishedGoodsRecallSettlements_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_FinishedGoodsRecallSettlements_CreatedById",
                table: "FinishedGoodsRecallSettlements",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_FinishedGoodsRecallSettlements_CurrencyId",
                table: "FinishedGoodsRecallSettlements",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_FinishedGoodsRecallSettlements_FinishedGoodsRecallId_Refere~",
                table: "FinishedGoodsRecallSettlements",
                columns: new[] { "FinishedGoodsRecallId", "Reference" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinishedGoodsRecallSettlements_InvoiceId",
                table: "FinishedGoodsRecallSettlements",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_FinishedGoodsRecallSettlements_LastDeletedById",
                table: "FinishedGoodsRecallSettlements",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_FinishedGoodsRecallSettlements_LastUpdatedById",
                table: "FinishedGoodsRecallSettlements",
                column: "LastUpdatedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FinishedGoodsCostingLine");

            migrationBuilder.DropTable(
                name: "FinishedGoodsRecallSettlements");

            migrationBuilder.DropColumn(
                name: "CostingConfirmedAt",
                table: "AllocateProductionOrders");

            migrationBuilder.DropColumn(
                name: "CostingConfirmedById",
                table: "AllocateProductionOrders");
        }
    }
}
