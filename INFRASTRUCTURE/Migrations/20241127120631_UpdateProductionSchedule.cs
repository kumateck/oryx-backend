using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class UpdateProductionSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductionSchedules_Resources_ResourceId",
                table: "ProductionSchedules");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionSchedules_WorkOrders_WorkOrderId",
                table: "ProductionSchedules");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkOrders_Products_ProductId",
                table: "WorkOrders");

            migrationBuilder.DropIndex(
                name: "IX_WorkOrders_ProductId",
                table: "WorkOrders");

            migrationBuilder.DropIndex(
                name: "IX_ProductionSchedules_WorkOrderId",
                table: "ProductionSchedules");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "WorkOrderId",
                table: "ProductionSchedules");

            migrationBuilder.RenameColumn(
                name: "ResourceId",
                table: "ProductionSchedules",
                newName: "ProductId");

            migrationBuilder.RenameIndex(
                name: "IX_ProductionSchedules_ResourceId",
                table: "ProductionSchedules",
                newName: "IX_ProductionSchedules_ProductId");

            migrationBuilder.AddColumn<Guid>(
                name: "ProductionScheduleId",
                table: "WorkOrders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "ProductionSchedules",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ProductionScheduleItem",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductionScheduleId = table.Column<Guid>(type: "uuid", nullable: false),
                    UomId = table.Column<Guid>(type: "uuid", nullable: true),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionScheduleItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionScheduleItem_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductionScheduleItem_ProductionSchedules_ProductionSchedu~",
                        column: x => x.ProductionScheduleId,
                        principalTable: "ProductionSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductionScheduleItem_UnitOfMeasures_UomId",
                        column: x => x.UomId,
                        principalTable: "UnitOfMeasures",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductionScheduleItem_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductionScheduleItem_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductionScheduleItem_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_ProductionScheduleId",
                table: "WorkOrders",
                column: "ProductionScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionScheduleItem_CreatedById",
                table: "ProductionScheduleItem",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionScheduleItem_LastDeletedById",
                table: "ProductionScheduleItem",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionScheduleItem_LastUpdatedById",
                table: "ProductionScheduleItem",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionScheduleItem_MaterialId",
                table: "ProductionScheduleItem",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionScheduleItem_ProductionScheduleId",
                table: "ProductionScheduleItem",
                column: "ProductionScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionScheduleItem_UomId",
                table: "ProductionScheduleItem",
                column: "UomId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionSchedules_Products_ProductId",
                table: "ProductionSchedules",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkOrders_ProductionSchedules_ProductionScheduleId",
                table: "WorkOrders",
                column: "ProductionScheduleId",
                principalTable: "ProductionSchedules",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductionSchedules_Products_ProductId",
                table: "ProductionSchedules");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkOrders_ProductionSchedules_ProductionScheduleId",
                table: "WorkOrders");

            migrationBuilder.DropTable(
                name: "ProductionScheduleItem");

            migrationBuilder.DropIndex(
                name: "IX_WorkOrders_ProductionScheduleId",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "ProductionScheduleId",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "ProductionSchedules");

            migrationBuilder.RenameColumn(
                name: "ProductId",
                table: "ProductionSchedules",
                newName: "ResourceId");

            migrationBuilder.RenameIndex(
                name: "IX_ProductionSchedules_ProductId",
                table: "ProductionSchedules",
                newName: "IX_ProductionSchedules_ResourceId");

            migrationBuilder.AddColumn<Guid>(
                name: "ProductId",
                table: "WorkOrders",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "WorkOrderId",
                table: "ProductionSchedules",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_ProductId",
                table: "WorkOrders",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionSchedules_WorkOrderId",
                table: "ProductionSchedules",
                column: "WorkOrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionSchedules_Resources_ResourceId",
                table: "ProductionSchedules",
                column: "ResourceId",
                principalTable: "Resources",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionSchedules_WorkOrders_WorkOrderId",
                table: "ProductionSchedules",
                column: "WorkOrderId",
                principalTable: "WorkOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkOrders_Products_ProductId",
                table: "WorkOrders",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
