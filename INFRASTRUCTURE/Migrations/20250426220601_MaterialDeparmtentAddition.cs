using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class MaterialDeparmtentAddition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaximumStockLevel",
                table: "Materials");

            migrationBuilder.DropColumn(
                name: "MinimumStockLevel",
                table: "Materials");

            migrationBuilder.DropColumn(
                name: "ReOrderLevel",
                table: "Materials");

            migrationBuilder.CreateTable(
                name: "MaterialDepartments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReOrderLevel = table.Column<int>(type: "integer", nullable: false),
                    MinimumStockLevel = table.Column<int>(type: "integer", nullable: false),
                    MaximumStockLevel = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialDepartments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaterialDepartments_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MaterialDepartments_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MaterialDepartments_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MaterialDepartments_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MaterialDepartments_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_MaterialDepartments_CreatedById",
                table: "MaterialDepartments",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialDepartments_DepartmentId",
                table: "MaterialDepartments",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialDepartments_LastDeletedById",
                table: "MaterialDepartments",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialDepartments_LastUpdatedById",
                table: "MaterialDepartments",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialDepartments_MaterialId",
                table: "MaterialDepartments",
                column: "MaterialId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MaterialDepartments");

            migrationBuilder.AddColumn<int>(
                name: "MaximumStockLevel",
                table: "Materials",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MinimumStockLevel",
                table: "Materials",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ReOrderLevel",
                table: "Materials",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
