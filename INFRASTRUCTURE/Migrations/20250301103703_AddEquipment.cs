using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddEquipment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PackingExcessMargin",
                table: "Products");

            migrationBuilder.AddColumn<Guid>(
                name: "EquipmentId",
                table: "Products",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PackingExcessMargin",
                table: "ProductPackages",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "Equipments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    MachineId = table.Column<string>(type: "text", nullable: true),
                    IsStorage = table.Column<bool>(type: "boolean", nullable: false),
                    CapacityQuantity = table.Column<decimal>(type: "numeric", nullable: false),
                    UoMId = table.Column<Guid>(type: "uuid", nullable: false),
                    RelevanceCheck = table.Column<bool>(type: "boolean", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    StorageLocation = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Equipments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Equipments_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Equipments_UnitOfMeasures_UoMId",
                        column: x => x.UoMId,
                        principalTable: "UnitOfMeasures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Equipments_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Equipments_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Equipments_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Products_EquipmentId",
                table: "Products",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Equipments_CreatedById",
                table: "Equipments",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Equipments_DepartmentId",
                table: "Equipments",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Equipments_LastDeletedById",
                table: "Equipments",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_Equipments_LastUpdatedById",
                table: "Equipments",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Equipments_UoMId",
                table: "Equipments",
                column: "UoMId");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Equipments_EquipmentId",
                table: "Products",
                column: "EquipmentId",
                principalTable: "Equipments",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_Equipments_EquipmentId",
                table: "Products");

            migrationBuilder.DropTable(
                name: "Equipments");

            migrationBuilder.DropIndex(
                name: "IX_Products_EquipmentId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "EquipmentId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "PackingExcessMargin",
                table: "ProductPackages");

            migrationBuilder.AddColumn<decimal>(
                name: "PackingExcessMargin",
                table: "Products",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
