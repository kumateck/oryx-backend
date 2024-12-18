using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class DepartmentToMultipleWarehouses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Departments_Warehouses_WarehouseId",
                table: "Departments");

            migrationBuilder.DropIndex(
                name: "IX_Departments_WarehouseId",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "WarehouseId",
                table: "Departments");

            migrationBuilder.CreateTable(
                name: "DepartmentWarehouses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DepartmentWarehouses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DepartmentWarehouses_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DepartmentWarehouses_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DepartmentWarehouses_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DepartmentWarehouses_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DepartmentWarehouses_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentWarehouses_CreatedById",
                table: "DepartmentWarehouses",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentWarehouses_DepartmentId",
                table: "DepartmentWarehouses",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentWarehouses_LastDeletedById",
                table: "DepartmentWarehouses",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentWarehouses_LastUpdatedById",
                table: "DepartmentWarehouses",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentWarehouses_WarehouseId",
                table: "DepartmentWarehouses",
                column: "WarehouseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DepartmentWarehouses");

            migrationBuilder.AddColumn<Guid>(
                name: "WarehouseId",
                table: "Departments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Departments_WarehouseId",
                table: "Departments",
                column: "WarehouseId");

            migrationBuilder.AddForeignKey(
                name: "FK_Departments_Warehouses_WarehouseId",
                table: "Departments",
                column: "WarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id");
        }
    }
}
