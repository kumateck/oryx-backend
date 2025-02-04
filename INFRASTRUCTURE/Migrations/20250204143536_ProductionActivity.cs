using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class ProductionActivity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "ProductionSchedules");

            migrationBuilder.AddColumn<decimal>(
                name: "Quantity",
                table: "ProductionScheduleProducts",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "ProductionActivities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ProductionScheduleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionActivities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionActivities_ProductionSchedules_ProductionSchedule~",
                        column: x => x.ProductionScheduleId,
                        principalTable: "ProductionSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductionActivities_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductionActivities_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductionActivities_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductionActivities_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ProductionActivitySteps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductionActivityId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowId = table.Column<Guid>(type: "uuid", nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionActivitySteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionActivitySteps_Forms_WorkflowId",
                        column: x => x.WorkflowId,
                        principalTable: "Forms",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductionActivitySteps_Operations_OperationId",
                        column: x => x.OperationId,
                        principalTable: "Operations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductionActivitySteps_ProductionActivities_ProductionActi~",
                        column: x => x.ProductionActivityId,
                        principalTable: "ProductionActivities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductionActivitySteps_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductionActivitySteps_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductionActivitySteps_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ProductionActivityStepResources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductionActivityStepId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_ProductionActivityStepResources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionActivityStepResources_ProductionActivitySteps_Pro~",
                        column: x => x.ProductionActivityStepId,
                        principalTable: "ProductionActivitySteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductionActivityStepResources_Resources_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "Resources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductionActivityStepResources_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductionActivityStepResources_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductionActivityStepResources_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ProductionActivityStepUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductionActivityStepId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionActivityStepUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionActivityStepUsers_ProductionActivitySteps_Product~",
                        column: x => x.ProductionActivityStepId,
                        principalTable: "ProductionActivitySteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductionActivityStepUsers_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductionActivityStepUsers_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductionActivityStepUsers_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductionActivityStepUsers_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductionActivityStepWorkCenters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductionActivityStepId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkCenterId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionActivityStepWorkCenters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionActivityStepWorkCenters_ProductionActivitySteps_P~",
                        column: x => x.ProductionActivityStepId,
                        principalTable: "ProductionActivitySteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductionActivityStepWorkCenters_WorkCenters_WorkCenterId",
                        column: x => x.WorkCenterId,
                        principalTable: "WorkCenters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductionActivityStepWorkCenters_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductionActivityStepWorkCenters_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductionActivityStepWorkCenters_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionActivities_CreatedById",
                table: "ProductionActivities",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionActivities_LastDeletedById",
                table: "ProductionActivities",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionActivities_LastUpdatedById",
                table: "ProductionActivities",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionActivities_ProductId",
                table: "ProductionActivities",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionActivities_ProductionScheduleId",
                table: "ProductionActivities",
                column: "ProductionScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionActivityStepResources_CreatedById",
                table: "ProductionActivityStepResources",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionActivityStepResources_LastDeletedById",
                table: "ProductionActivityStepResources",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionActivityStepResources_LastUpdatedById",
                table: "ProductionActivityStepResources",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionActivityStepResources_ProductionActivityStepId",
                table: "ProductionActivityStepResources",
                column: "ProductionActivityStepId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionActivityStepResources_ResourceId",
                table: "ProductionActivityStepResources",
                column: "ResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionActivitySteps_CreatedById",
                table: "ProductionActivitySteps",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionActivitySteps_LastDeletedById",
                table: "ProductionActivitySteps",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionActivitySteps_LastUpdatedById",
                table: "ProductionActivitySteps",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionActivitySteps_OperationId",
                table: "ProductionActivitySteps",
                column: "OperationId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionActivitySteps_ProductionActivityId",
                table: "ProductionActivitySteps",
                column: "ProductionActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionActivitySteps_WorkflowId",
                table: "ProductionActivitySteps",
                column: "WorkflowId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionActivityStepUsers_CreatedById",
                table: "ProductionActivityStepUsers",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionActivityStepUsers_LastDeletedById",
                table: "ProductionActivityStepUsers",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionActivityStepUsers_LastUpdatedById",
                table: "ProductionActivityStepUsers",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionActivityStepUsers_ProductionActivityStepId",
                table: "ProductionActivityStepUsers",
                column: "ProductionActivityStepId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionActivityStepUsers_UserId",
                table: "ProductionActivityStepUsers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionActivityStepWorkCenters_CreatedById",
                table: "ProductionActivityStepWorkCenters",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionActivityStepWorkCenters_LastDeletedById",
                table: "ProductionActivityStepWorkCenters",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionActivityStepWorkCenters_LastUpdatedById",
                table: "ProductionActivityStepWorkCenters",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionActivityStepWorkCenters_ProductionActivityStepId",
                table: "ProductionActivityStepWorkCenters",
                column: "ProductionActivityStepId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionActivityStepWorkCenters_WorkCenterId",
                table: "ProductionActivityStepWorkCenters",
                column: "WorkCenterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductionActivityStepResources");

            migrationBuilder.DropTable(
                name: "ProductionActivityStepUsers");

            migrationBuilder.DropTable(
                name: "ProductionActivityStepWorkCenters");

            migrationBuilder.DropTable(
                name: "ProductionActivitySteps");

            migrationBuilder.DropTable(
                name: "ProductionActivities");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "ProductionScheduleProducts");

            migrationBuilder.AddColumn<decimal>(
                name: "Quantity",
                table: "ProductionSchedules",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
