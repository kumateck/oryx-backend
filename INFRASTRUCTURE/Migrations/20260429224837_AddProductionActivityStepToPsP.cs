using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddProductionActivityStepToPsP : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductionActivities_ProductionScheduleProductId",
                table: "ProductionActivities");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionActivities_ProductionScheduleProductId",
                table: "ProductionActivities",
                column: "ProductionScheduleProductId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductionActivities_ProductionScheduleProductId",
                table: "ProductionActivities");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionActivities_ProductionScheduleProductId",
                table: "ProductionActivities",
                column: "ProductionScheduleProductId");
        }
    }
}
