using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddQcEquipment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QcEquipmentCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QcEquipmentCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QcEquipmentCategories_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_QcEquipmentCategories_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_QcEquipmentCategories_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "QcEquipments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    QcEquipmentCategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    SerialNumber = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    Make = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    Model = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QcEquipments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QcEquipments_QcEquipmentCategories_QcEquipmentCategoryId",
                        column: x => x.QcEquipmentCategoryId,
                        principalTable: "QcEquipmentCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QcEquipments_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_QcEquipments_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_QcEquipments_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_QcEquipmentCategories_CreatedById",
                table: "QcEquipmentCategories",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcEquipmentCategories_LastDeletedById",
                table: "QcEquipmentCategories",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcEquipmentCategories_LastUpdatedById",
                table: "QcEquipmentCategories",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcEquipments_CreatedById",
                table: "QcEquipments",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcEquipments_LastDeletedById",
                table: "QcEquipments",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcEquipments_LastUpdatedById",
                table: "QcEquipments",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcEquipments_QcEquipmentCategoryId",
                table: "QcEquipments",
                column: "QcEquipmentCategoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QcEquipments");

            migrationBuilder.DropTable(
                name: "QcEquipmentCategories");
        }
    }
}
