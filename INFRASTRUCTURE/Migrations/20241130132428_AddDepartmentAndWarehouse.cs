using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddDepartmentAndWarehouse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_users_users_DirectReportId",
                table: "users");

            migrationBuilder.DropColumn(
                name: "EmployeeId",
                table: "users");

            migrationBuilder.DropColumn(
                name: "HiredOn",
                table: "users");

            migrationBuilder.DropColumn(
                name: "Sex",
                table: "users");

            migrationBuilder.RenameColumn(
                name: "DirectReportId",
                table: "users",
                newName: "DepartmentId");

            migrationBuilder.RenameIndex(
                name: "IX_users_DirectReportId",
                table: "users",
                newName: "IX_users_DepartmentId");

            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Departments_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Departments_WarehouseId",
                table: "Departments",
                column: "WarehouseId");

            migrationBuilder.AddForeignKey(
                name: "FK_users_Departments_DepartmentId",
                table: "users",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_users_Departments_DepartmentId",
                table: "users");

            migrationBuilder.DropTable(
                name: "Departments");

            migrationBuilder.RenameColumn(
                name: "DepartmentId",
                table: "users",
                newName: "DirectReportId");

            migrationBuilder.RenameIndex(
                name: "IX_users_DepartmentId",
                table: "users",
                newName: "IX_users_DirectReportId");

            migrationBuilder.AddColumn<string>(
                name: "EmployeeId",
                table: "users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "HiredOn",
                table: "users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Sex",
                table: "users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_users_users_DirectReportId",
                table: "users",
                column: "DirectReportId",
                principalTable: "users",
                principalColumn: "Id");
        }
    }
}
