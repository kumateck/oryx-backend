using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class RenameNonProductionSupplierToVendor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Items_NonProductionSuppliers_NonProductionSupplierId",
                table: "Items");

            migrationBuilder.DropTable(
                name: "NonProductionSuppliers");

            migrationBuilder.RenameColumn(
                name: "NonProductionSupplierId",
                table: "Items",
                newName: "VendorId");

            migrationBuilder.RenameIndex(
                name: "IX_Items_NonProductionSupplierId",
                table: "Items",
                newName: "IX_Items_VendorId");

            migrationBuilder.CreateTable(
                name: "Vendors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Address = table.Column<string>(type: "text", nullable: true),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: true),
                    CountryId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrencyId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vendors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Vendors_Countries_CountryId",
                        column: x => x.CountryId,
                        principalTable: "Countries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Vendors_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Vendors_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Vendors_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Vendors_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Vendors_CountryId",
                table: "Vendors",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_Vendors_CreatedById",
                table: "Vendors",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Vendors_CurrencyId",
                table: "Vendors",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_Vendors_LastDeletedById",
                table: "Vendors",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_Vendors_LastUpdatedById",
                table: "Vendors",
                column: "LastUpdatedById");

            migrationBuilder.AddForeignKey(
                name: "FK_Items_Vendors_VendorId",
                table: "Items",
                column: "VendorId",
                principalTable: "Vendors",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Items_Vendors_VendorId",
                table: "Items");

            migrationBuilder.DropTable(
                name: "Vendors");

            migrationBuilder.RenameColumn(
                name: "VendorId",
                table: "Items",
                newName: "NonProductionSupplierId");

            migrationBuilder.RenameIndex(
                name: "IX_Items_VendorId",
                table: "Items",
                newName: "IX_Items_NonProductionSupplierId");

            migrationBuilder.CreateTable(
                name: "NonProductionSuppliers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CountryId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    CurrencyId = table.Column<Guid>(type: "uuid", nullable: false),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    Address = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NonProductionSuppliers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NonProductionSuppliers_Countries_CountryId",
                        column: x => x.CountryId,
                        principalTable: "Countries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NonProductionSuppliers_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NonProductionSuppliers_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_NonProductionSuppliers_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_NonProductionSuppliers_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_NonProductionSuppliers_CountryId",
                table: "NonProductionSuppliers",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_NonProductionSuppliers_CreatedById",
                table: "NonProductionSuppliers",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_NonProductionSuppliers_CurrencyId",
                table: "NonProductionSuppliers",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_NonProductionSuppliers_LastDeletedById",
                table: "NonProductionSuppliers",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_NonProductionSuppliers_LastUpdatedById",
                table: "NonProductionSuppliers",
                column: "LastUpdatedById");

            migrationBuilder.AddForeignKey(
                name: "FK_Items_NonProductionSuppliers_NonProductionSupplierId",
                table: "Items",
                column: "NonProductionSupplierId",
                principalTable: "NonProductionSuppliers",
                principalColumn: "Id");
        }
    }
}
