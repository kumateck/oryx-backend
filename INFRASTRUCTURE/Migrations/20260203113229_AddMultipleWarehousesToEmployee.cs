using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddMultipleWarehousesToEmployee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Employees_Warehouses_WarehouseId",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Employees_WarehouseId",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "WarehouseId",
                table: "Employees");

            migrationBuilder.CreateTable(
                name: "EmployeeWarehouse",
                columns: table => new
                {
                    EmployeesId = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehousesId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeWarehouse", x => new { x.EmployeesId, x.WarehousesId });
                    table.ForeignKey(
                        name: "FK_EmployeeWarehouse_Employees_EmployeesId",
                        column: x => x.EmployeesId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeWarehouse_Warehouses_WarehousesId",
                        column: x => x.WarehousesId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeWarehouse_WarehousesId",
                table: "EmployeeWarehouse",
                column: "WarehousesId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmployeeWarehouse");

            migrationBuilder.AddColumn<Guid>(
                name: "WarehouseId",
                table: "Employees",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_WarehouseId",
                table: "Employees",
                column: "WarehouseId");

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_Warehouses_WarehouseId",
                table: "Employees",
                column: "WarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id");
        }
    }
}
