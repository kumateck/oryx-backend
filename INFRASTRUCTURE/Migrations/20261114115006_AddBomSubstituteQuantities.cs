using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddBomSubstituteQuantities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "BaseQuantity",
                table: "BillOfMaterialItemSubstitutes",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BaseUoMId",
                table: "BillOfMaterialItemSubstitutes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PrescribedQuantity",
                table: "BillOfMaterialItemSubstitutes",
                type: "numeric",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "StockRequisitionMaterialChoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequisitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequisitionItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductionScheduleProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceMaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromMaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromQuantity = table.Column<decimal>(type: "numeric", nullable: false),
                    FromUnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    ToMaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    ToQuantity = table.Column<decimal>(type: "numeric", nullable: false),
                    ToUnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChosenById = table.Column<Guid>(type: "uuid", nullable: false),
                    ChosenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockRequisitionMaterialChoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockRequisitionMaterialChoices_Materials_FromMaterialId",
                        column: x => x.FromMaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockRequisitionMaterialChoices_Materials_SourceMaterialId",
                        column: x => x.SourceMaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockRequisitionMaterialChoices_Materials_ToMaterialId",
                        column: x => x.ToMaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockRequisitionMaterialChoices_Requisitions_RequisitionId",
                        column: x => x.RequisitionId,
                        principalTable: "Requisitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockRequisitionMaterialChoices_users_ChosenById",
                        column: x => x.ChosenById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockRequisitionMaterialChoices_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockRequisitionMaterialChoices_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockRequisitionMaterialChoices_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_BillOfMaterialItemSubstitutes_BaseUoMId",
                table: "BillOfMaterialItemSubstitutes",
                column: "BaseUoMId");

            migrationBuilder.CreateIndex(
                name: "IX_StockRequisitionMaterialChoices_ChosenById",
                table: "StockRequisitionMaterialChoices",
                column: "ChosenById");

            migrationBuilder.CreateIndex(
                name: "IX_StockRequisitionMaterialChoices_CreatedById",
                table: "StockRequisitionMaterialChoices",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_StockRequisitionMaterialChoices_FromMaterialId",
                table: "StockRequisitionMaterialChoices",
                column: "FromMaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_StockRequisitionMaterialChoices_LastDeletedById",
                table: "StockRequisitionMaterialChoices",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_StockRequisitionMaterialChoices_LastUpdatedById",
                table: "StockRequisitionMaterialChoices",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_StockRequisitionMaterialChoices_RequisitionId_ChosenAt",
                table: "StockRequisitionMaterialChoices",
                columns: new[] { "RequisitionId", "ChosenAt" });

            migrationBuilder.CreateIndex(
                name: "IX_StockRequisitionMaterialChoices_SourceMaterialId",
                table: "StockRequisitionMaterialChoices",
                column: "SourceMaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_StockRequisitionMaterialChoices_ToMaterialId",
                table: "StockRequisitionMaterialChoices",
                column: "ToMaterialId");

            migrationBuilder.AddForeignKey(
                name: "FK_BillOfMaterialItemSubstitutes_UnitOfMeasures_BaseUoMId",
                table: "BillOfMaterialItemSubstitutes",
                column: "BaseUoMId",
                principalTable: "UnitOfMeasures",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BillOfMaterialItemSubstitutes_UnitOfMeasures_BaseUoMId",
                table: "BillOfMaterialItemSubstitutes");

            migrationBuilder.DropTable(
                name: "StockRequisitionMaterialChoices");

            migrationBuilder.DropIndex(
                name: "IX_BillOfMaterialItemSubstitutes_BaseUoMId",
                table: "BillOfMaterialItemSubstitutes");

            migrationBuilder.DropColumn(
                name: "BaseQuantity",
                table: "BillOfMaterialItemSubstitutes");

            migrationBuilder.DropColumn(
                name: "BaseUoMId",
                table: "BillOfMaterialItemSubstitutes");

            migrationBuilder.DropColumn(
                name: "PrescribedQuantity",
                table: "BillOfMaterialItemSubstitutes");
        }
    }
}
