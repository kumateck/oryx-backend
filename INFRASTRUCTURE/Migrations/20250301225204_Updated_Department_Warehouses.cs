using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class Updated_Department_Warehouses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DepartmentWarehouses");

            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId",
                table: "Warehouses",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Departments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_DepartmentId",
                table: "Warehouses",
                column: "DepartmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Warehouses_Departments_DepartmentId",
                table: "Warehouses",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Warehouses_Departments_DepartmentId",
                table: "Warehouses");

            migrationBuilder.DropIndex(
                name: "IX_Warehouses_DepartmentId",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Departments");

            migrationBuilder.CreateTable(
                name: "DepartmentWarehouses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
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
    }
}
