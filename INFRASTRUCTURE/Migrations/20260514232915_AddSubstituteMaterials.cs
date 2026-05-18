using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddSubstituteMaterials : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BillOfMaterialItemSubstitutes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BillOfMaterialItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubstituteMaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillOfMaterialItemSubstitutes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BillOfMaterialItemSubstitutes_BillOfMaterialItems_BillOfMat~",
                        column: x => x.BillOfMaterialItemId,
                        principalTable: "BillOfMaterialItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BillOfMaterialItemSubstitutes_Materials_SubstituteMaterialId",
                        column: x => x.SubstituteMaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BillOfMaterialItemSubstitutes_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BillOfMaterialItemSubstitutes_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BillOfMaterialItemSubstitutes_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ProductPackageSubstitutes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductPackageId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubstituteMaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductPackageSubstitutes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductPackageSubstitutes_Materials_SubstituteMaterialId",
                        column: x => x.SubstituteMaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductPackageSubstitutes_ProductPackages_ProductPackageId",
                        column: x => x.ProductPackageId,
                        principalTable: "ProductPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductPackageSubstitutes_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductPackageSubstitutes_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductPackageSubstitutes_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_BillOfMaterialItemSubstitutes_BillOfMaterialItemId",
                table: "BillOfMaterialItemSubstitutes",
                column: "BillOfMaterialItemId");

            migrationBuilder.CreateIndex(
                name: "IX_BillOfMaterialItemSubstitutes_CreatedById",
                table: "BillOfMaterialItemSubstitutes",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_BillOfMaterialItemSubstitutes_LastDeletedById",
                table: "BillOfMaterialItemSubstitutes",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_BillOfMaterialItemSubstitutes_LastUpdatedById",
                table: "BillOfMaterialItemSubstitutes",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_BillOfMaterialItemSubstitutes_SubstituteMaterialId",
                table: "BillOfMaterialItemSubstitutes",
                column: "SubstituteMaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductPackageSubstitutes_CreatedById",
                table: "ProductPackageSubstitutes",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductPackageSubstitutes_LastDeletedById",
                table: "ProductPackageSubstitutes",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductPackageSubstitutes_LastUpdatedById",
                table: "ProductPackageSubstitutes",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductPackageSubstitutes_ProductPackageId",
                table: "ProductPackageSubstitutes",
                column: "ProductPackageId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductPackageSubstitutes_SubstituteMaterialId",
                table: "ProductPackageSubstitutes",
                column: "SubstituteMaterialId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BillOfMaterialItemSubstitutes");

            migrationBuilder.DropTable(
                name: "ProductPackageSubstitutes");
        }
    }
}
