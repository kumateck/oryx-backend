using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddInventory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaterialSpecifications_MaterialAnalyticalRawData_MaterialAn~",
                table: "MaterialSpecifications");

            migrationBuilder.DropTable(
                name: "MaterialSpecifications_TestSpecifications");

            migrationBuilder.DropTable(
                name: "ProductSpecifications_TestSpecifications");

            migrationBuilder.RenameColumn(
                name: "MaterialAnalyticalRawDataId",
                table: "MaterialSpecifications",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_MaterialSpecifications_MaterialAnalyticalRawDataId",
                table: "MaterialSpecifications",
                newName: "IX_MaterialSpecifications_UserId");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "ProductSpecifications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DueDate",
                table: "ProductSpecifications",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "FormId",
                table: "ProductSpecifications",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "ProductSpecifications",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "MaterialSpecifications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DueDate",
                table: "MaterialSpecifications",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "Items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialName = table.Column<string>(type: "text", nullable: true),
                    Code = table.Column<string>(type: "text", nullable: true),
                    Classification = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: true),
                    UnitOfMeasureId = table.Column<Guid>(type: "uuid", nullable: false),
                    HasBatchNumber = table.Column<bool>(type: "boolean", nullable: false),
                    MaterialBatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    Remarks = table.Column<string>(type: "text", nullable: true),
                    ReorderRule = table.Column<int>(type: "integer", nullable: false),
                    InitialStockQuantity = table.Column<decimal>(type: "numeric", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inventories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Inventories_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Inventories_MaterialBatches_MaterialBatchId",
                        column: x => x.MaterialBatchId,
                        principalTable: "MaterialBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Inventories_UnitOfMeasures_UnitOfMeasureId",
                        column: x => x.UnitOfMeasureId,
                        principalTable: "UnitOfMeasures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Inventories_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Inventories_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Inventories_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductSpecifications_FormId",
                table: "ProductSpecifications",
                column: "FormId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSpecifications_UserId",
                table: "ProductSpecifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Inventories_CreatedById",
                table: "Items",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Inventories_DepartmentId",
                table: "Items",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Inventories_LastDeletedById",
                table: "Items",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_Inventories_LastUpdatedById",
                table: "Items",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Inventories_MaterialBatchId",
                table: "Items",
                column: "MaterialBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_Inventories_UnitOfMeasureId",
                table: "Items",
                column: "UnitOfMeasureId");

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialSpecifications_users_UserId",
                table: "MaterialSpecifications",
                column: "UserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductSpecifications_Forms_FormId",
                table: "ProductSpecifications",
                column: "FormId",
                principalTable: "Forms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductSpecifications_users_UserId",
                table: "ProductSpecifications",
                column: "UserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaterialSpecifications_users_UserId",
                table: "MaterialSpecifications");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductSpecifications_Forms_FormId",
                table: "ProductSpecifications");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductSpecifications_users_UserId",
                table: "ProductSpecifications");

            migrationBuilder.DropTable(
                name: "Items");

            migrationBuilder.DropIndex(
                name: "IX_ProductSpecifications_FormId",
                table: "ProductSpecifications");

            migrationBuilder.DropIndex(
                name: "IX_ProductSpecifications_UserId",
                table: "ProductSpecifications");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "ProductSpecifications");

            migrationBuilder.DropColumn(
                name: "DueDate",
                table: "ProductSpecifications");

            migrationBuilder.DropColumn(
                name: "FormId",
                table: "ProductSpecifications");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "ProductSpecifications");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "MaterialSpecifications");

            migrationBuilder.DropColumn(
                name: "DueDate",
                table: "MaterialSpecifications");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "MaterialSpecifications",
                newName: "MaterialAnalyticalRawDataId");

            migrationBuilder.RenameIndex(
                name: "IX_MaterialSpecifications_UserId",
                table: "MaterialSpecifications",
                newName: "IX_MaterialSpecifications_MaterialAnalyticalRawDataId");

            migrationBuilder.CreateTable(
                name: "MaterialSpecifications_TestSpecifications",
                columns: table => new
                {
                    MaterialSpecificationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Reference = table.Column<int>(type: "integer", nullable: false),
                    ReleaseSpecification = table.Column<string>(type: "text", nullable: true),
                    SrNumber = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialSpecifications_TestSpecifications", x => new { x.MaterialSpecificationId, x.Id });
                    table.ForeignKey(
                        name: "FK_MaterialSpecifications_TestSpecifications_MaterialSpecifica~",
                        column: x => x.MaterialSpecificationId,
                        principalTable: "MaterialSpecifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductSpecifications_TestSpecifications",
                columns: table => new
                {
                    ProductSpecificationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Reference = table.Column<int>(type: "integer", nullable: false),
                    ReleaseSpecification = table.Column<string>(type: "text", nullable: true),
                    SrNumber = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductSpecifications_TestSpecifications", x => new { x.ProductSpecificationId, x.Id });
                    table.ForeignKey(
                        name: "FK_ProductSpecifications_TestSpecifications_ProductSpecificati~",
                        column: x => x.ProductSpecificationId,
                        principalTable: "ProductSpecifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialSpecifications_MaterialAnalyticalRawData_MaterialAn~",
                table: "MaterialSpecifications",
                column: "MaterialAnalyticalRawDataId",
                principalTable: "MaterialAnalyticalRawData",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
