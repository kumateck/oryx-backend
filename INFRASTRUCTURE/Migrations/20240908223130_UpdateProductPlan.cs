using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DOMAIN.Migrations
{
    /// <inheritdoc />
    public partial class UpdateProductPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DosageForm",
                table: "Products",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Strength",
                table: "Products",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CasNumber",
                table: "BillOfMaterialItems",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Function",
                table: "BillOfMaterialItems",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Grade",
                table: "BillOfMaterialItems",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Order",
                table: "BillOfMaterialItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "BillOfMaterialItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Operations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Operations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Operations_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Operations_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Operations_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "WorkCenters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkCenters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkCenters_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WorkCenters_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WorkCenters_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Routes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkCenterId = table.Column<Guid>(type: "uuid", nullable: false),
                    BillOfMaterialItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    EstimatedTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Routes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Routes_BillOfMaterialItems_BillOfMaterialItemId",
                        column: x => x.BillOfMaterialItemId,
                        principalTable: "BillOfMaterialItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Routes_Operations_OperationId",
                        column: x => x.OperationId,
                        principalTable: "Operations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Routes_WorkCenters_WorkCenterId",
                        column: x => x.WorkCenterId,
                        principalTable: "WorkCenters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Routes_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Routes_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Routes_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RouteResources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RouteId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RouteResources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RouteResources_Resources_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "Resources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RouteResources_Routes_RouteId",
                        column: x => x.RouteId,
                        principalTable: "Routes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RouteResources_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RouteResources_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RouteResources_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Operations_CreatedById",
                table: "Operations",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Operations_LastDeletedById",
                table: "Operations",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_Operations_LastUpdatedById",
                table: "Operations",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RouteResources_CreatedById",
                table: "RouteResources",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RouteResources_LastDeletedById",
                table: "RouteResources",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_RouteResources_LastUpdatedById",
                table: "RouteResources",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RouteResources_ResourceId",
                table: "RouteResources",
                column: "ResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_RouteResources_RouteId",
                table: "RouteResources",
                column: "RouteId");

            migrationBuilder.CreateIndex(
                name: "IX_Routes_BillOfMaterialItemId",
                table: "Routes",
                column: "BillOfMaterialItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Routes_CreatedById",
                table: "Routes",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Routes_LastDeletedById",
                table: "Routes",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_Routes_LastUpdatedById",
                table: "Routes",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Routes_OperationId",
                table: "Routes",
                column: "OperationId");

            migrationBuilder.CreateIndex(
                name: "IX_Routes_WorkCenterId",
                table: "Routes",
                column: "WorkCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkCenters_CreatedById",
                table: "WorkCenters",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_WorkCenters_LastDeletedById",
                table: "WorkCenters",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_WorkCenters_LastUpdatedById",
                table: "WorkCenters",
                column: "LastUpdatedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RouteResources");

            migrationBuilder.DropTable(
                name: "Routes");

            migrationBuilder.DropTable(
                name: "Operations");

            migrationBuilder.DropTable(
                name: "WorkCenters");

            migrationBuilder.DropColumn(
                name: "DosageForm",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Strength",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CasNumber",
                table: "BillOfMaterialItems");

            migrationBuilder.DropColumn(
                name: "Function",
                table: "BillOfMaterialItems");

            migrationBuilder.DropColumn(
                name: "Grade",
                table: "BillOfMaterialItems");

            migrationBuilder.DropColumn(
                name: "Order",
                table: "BillOfMaterialItems");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "BillOfMaterialItems");
        }
    }
}
