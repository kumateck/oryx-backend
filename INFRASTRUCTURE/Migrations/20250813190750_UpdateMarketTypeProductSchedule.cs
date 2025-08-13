using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class UpdateMarketTypeProductSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductionScheduleProducts_MarketTypes_MarketTypeId",
                table: "ProductionScheduleProducts");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionScheduleProducts_Customers_MarketTypeId",
                table: "ProductionScheduleProducts",
                column: "MarketTypeId",
                principalTable: "Customers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductionScheduleProducts_Customers_MarketTypeId",
                table: "ProductionScheduleProducts");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionScheduleProducts_MarketTypes_MarketTypeId",
                table: "ProductionScheduleProducts",
                column: "MarketTypeId",
                principalTable: "MarketTypes",
                principalColumn: "Id");
        }
    }
}
