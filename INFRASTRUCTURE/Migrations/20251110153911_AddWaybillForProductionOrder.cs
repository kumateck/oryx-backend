using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddWaybillForProductionOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LoadedAt",
                table: "AllocateProductionOrders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "AllocateProductionOrders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "WaybillSentToCustomerAt",
                table: "AllocateProductionOrders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProductionOrderWaybills",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AllocateProductionOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Comment = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionOrderWaybills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionOrderWaybills_AllocateProductionOrders_AllocatePr~",
                        column: x => x.AllocateProductionOrderId,
                        principalTable: "AllocateProductionOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductionOrderWaybills_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductionOrderWaybills_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductionOrderWaybills_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrderWaybills_AllocateProductionOrderId",
                table: "ProductionOrderWaybills",
                column: "AllocateProductionOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrderWaybills_CreatedById",
                table: "ProductionOrderWaybills",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrderWaybills_LastDeletedById",
                table: "ProductionOrderWaybills",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrderWaybills_LastUpdatedById",
                table: "ProductionOrderWaybills",
                column: "LastUpdatedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductionOrderWaybills");

            migrationBuilder.DropColumn(
                name: "LoadedAt",
                table: "AllocateProductionOrders");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "AllocateProductionOrders");

            migrationBuilder.DropColumn(
                name: "WaybillSentToCustomerAt",
                table: "AllocateProductionOrders");
        }
    }
}
