using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddProductsToProductionSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductionSchedules_Products_ProductId",
                table: "ProductionSchedules");

            migrationBuilder.DropIndex(
                name: "IX_ProductionSchedules_ProductId",
                table: "ProductionSchedules");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "ProductionSchedules");

            migrationBuilder.CreateTable(
                name: "ProductionScheduleProducts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductionScheduleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionScheduleProducts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionScheduleProducts_ProductionSchedules_ProductionSc~",
                        column: x => x.ProductionScheduleId,
                        principalTable: "ProductionSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductionScheduleProducts_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionScheduleProducts_ProductId",
                table: "ProductionScheduleProducts",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionScheduleProducts_ProductionScheduleId",
                table: "ProductionScheduleProducts",
                column: "ProductionScheduleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductionScheduleProducts");

            migrationBuilder.AddColumn<Guid>(
                name: "ProductId",
                table: "ProductionSchedules",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductionSchedules_ProductId",
                table: "ProductionSchedules",
                column: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionSchedules_Products_ProductId",
                table: "ProductionSchedules",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id");
        }
    }
}
