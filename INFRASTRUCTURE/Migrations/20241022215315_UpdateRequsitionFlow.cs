using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class UpdateRequsitionFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaterialBatchConsumptions_MaterialBatches_BatchId",
                table: "MaterialBatchConsumptions");

            migrationBuilder.DropForeignKey(
                name: "FK_MaterialBatchConsumptions_users_CreatedById",
                table: "MaterialBatchConsumptions");

            migrationBuilder.DropForeignKey(
                name: "FK_MaterialBatchConsumptions_users_LastDeletedById",
                table: "MaterialBatchConsumptions");

            migrationBuilder.DropForeignKey(
                name: "FK_MaterialBatchConsumptions_users_LastUpdatedById",
                table: "MaterialBatchConsumptions");

            migrationBuilder.DropForeignKey(
                name: "FK_MaterialBatchConsumptions_users_UserId",
                table: "MaterialBatchConsumptions");

            migrationBuilder.DropForeignKey(
                name: "FK_Requisitions_Materials_MaterialId",
                table: "Requisitions");

            migrationBuilder.DropIndex(
                name: "IX_Requisitions_MaterialId",
                table: "Requisitions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MaterialBatchConsumptions",
                table: "MaterialBatchConsumptions");

            migrationBuilder.DropColumn(
                name: "MaterialId",
                table: "Requisitions");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "Requisitions");

            migrationBuilder.DropColumn(
                name: "RequestedAt",
                table: "Requisitions");

            migrationBuilder.RenameTable(
                name: "MaterialBatchConsumptions",
                newName: "MaterialBatchEvents");

            migrationBuilder.RenameIndex(
                name: "IX_MaterialBatchConsumptions_UserId",
                table: "MaterialBatchEvents",
                newName: "IX_MaterialBatchEvents_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_MaterialBatchConsumptions_LastUpdatedById",
                table: "MaterialBatchEvents",
                newName: "IX_MaterialBatchEvents_LastUpdatedById");

            migrationBuilder.RenameIndex(
                name: "IX_MaterialBatchConsumptions_LastDeletedById",
                table: "MaterialBatchEvents",
                newName: "IX_MaterialBatchEvents_LastDeletedById");

            migrationBuilder.RenameIndex(
                name: "IX_MaterialBatchConsumptions_CreatedById",
                table: "MaterialBatchEvents",
                newName: "IX_MaterialBatchEvents_CreatedById");

            migrationBuilder.RenameIndex(
                name: "IX_MaterialBatchConsumptions_BatchId",
                table: "MaterialBatchEvents",
                newName: "IX_MaterialBatchEvents_BatchId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MaterialBatchEvents",
                table: "MaterialBatchEvents",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "CompletedRequisitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequisitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedById = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RequisitionType = table.Column<int>(type: "integer", nullable: false),
                    Comments = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Approved = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompletedRequisitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompletedRequisitions_Requisitions_RequisitionId",
                        column: x => x.RequisitionId,
                        principalTable: "Requisitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CompletedRequisitions_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CompletedRequisitions_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CompletedRequisitions_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CompletedRequisitions_users_RequestedById",
                        column: x => x.RequestedById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RequisitionItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequisitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequisitionItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequisitionItems_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RequisitionItems_Requisitions_RequisitionId",
                        column: x => x.RequisitionId,
                        principalTable: "Requisitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RequisitionItems_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RequisitionItems_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RequisitionItems_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CompletedRequisitionItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompletedRequisitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompletedRequisitionItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompletedRequisitionItems_CompletedRequisitions_CompletedRe~",
                        column: x => x.CompletedRequisitionId,
                        principalTable: "CompletedRequisitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CompletedRequisitionItems_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CompletedRequisitionItems_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CompletedRequisitionItems_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CompletedRequisitionItems_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompletedRequisitionItems_CompletedRequisitionId",
                table: "CompletedRequisitionItems",
                column: "CompletedRequisitionId");

            migrationBuilder.CreateIndex(
                name: "IX_CompletedRequisitionItems_CreatedById",
                table: "CompletedRequisitionItems",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_CompletedRequisitionItems_LastDeletedById",
                table: "CompletedRequisitionItems",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_CompletedRequisitionItems_LastUpdatedById",
                table: "CompletedRequisitionItems",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_CompletedRequisitionItems_MaterialId",
                table: "CompletedRequisitionItems",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_CompletedRequisitions_CreatedById",
                table: "CompletedRequisitions",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_CompletedRequisitions_LastDeletedById",
                table: "CompletedRequisitions",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_CompletedRequisitions_LastUpdatedById",
                table: "CompletedRequisitions",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_CompletedRequisitions_RequestedById",
                table: "CompletedRequisitions",
                column: "RequestedById");

            migrationBuilder.CreateIndex(
                name: "IX_CompletedRequisitions_RequisitionId",
                table: "CompletedRequisitions",
                column: "RequisitionId");

            migrationBuilder.CreateIndex(
                name: "IX_RequisitionItems_CreatedById",
                table: "RequisitionItems",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RequisitionItems_LastDeletedById",
                table: "RequisitionItems",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_RequisitionItems_LastUpdatedById",
                table: "RequisitionItems",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RequisitionItems_MaterialId",
                table: "RequisitionItems",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_RequisitionItems_RequisitionId",
                table: "RequisitionItems",
                column: "RequisitionId");

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialBatchEvents_MaterialBatches_BatchId",
                table: "MaterialBatchEvents",
                column: "BatchId",
                principalTable: "MaterialBatches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialBatchEvents_users_CreatedById",
                table: "MaterialBatchEvents",
                column: "CreatedById",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialBatchEvents_users_LastDeletedById",
                table: "MaterialBatchEvents",
                column: "LastDeletedById",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialBatchEvents_users_LastUpdatedById",
                table: "MaterialBatchEvents",
                column: "LastUpdatedById",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialBatchEvents_users_UserId",
                table: "MaterialBatchEvents",
                column: "UserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaterialBatchEvents_MaterialBatches_BatchId",
                table: "MaterialBatchEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_MaterialBatchEvents_users_CreatedById",
                table: "MaterialBatchEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_MaterialBatchEvents_users_LastDeletedById",
                table: "MaterialBatchEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_MaterialBatchEvents_users_LastUpdatedById",
                table: "MaterialBatchEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_MaterialBatchEvents_users_UserId",
                table: "MaterialBatchEvents");

            migrationBuilder.DropTable(
                name: "CompletedRequisitionItems");

            migrationBuilder.DropTable(
                name: "RequisitionItems");

            migrationBuilder.DropTable(
                name: "CompletedRequisitions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MaterialBatchEvents",
                table: "MaterialBatchEvents");

            migrationBuilder.RenameTable(
                name: "MaterialBatchEvents",
                newName: "MaterialBatchConsumptions");

            migrationBuilder.RenameIndex(
                name: "IX_MaterialBatchEvents_UserId",
                table: "MaterialBatchConsumptions",
                newName: "IX_MaterialBatchConsumptions_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_MaterialBatchEvents_LastUpdatedById",
                table: "MaterialBatchConsumptions",
                newName: "IX_MaterialBatchConsumptions_LastUpdatedById");

            migrationBuilder.RenameIndex(
                name: "IX_MaterialBatchEvents_LastDeletedById",
                table: "MaterialBatchConsumptions",
                newName: "IX_MaterialBatchConsumptions_LastDeletedById");

            migrationBuilder.RenameIndex(
                name: "IX_MaterialBatchEvents_CreatedById",
                table: "MaterialBatchConsumptions",
                newName: "IX_MaterialBatchConsumptions_CreatedById");

            migrationBuilder.RenameIndex(
                name: "IX_MaterialBatchEvents_BatchId",
                table: "MaterialBatchConsumptions",
                newName: "IX_MaterialBatchConsumptions_BatchId");

            migrationBuilder.AddColumn<Guid>(
                name: "MaterialId",
                table: "Requisitions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "Requisitions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "RequestedAt",
                table: "Requisitions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddPrimaryKey(
                name: "PK_MaterialBatchConsumptions",
                table: "MaterialBatchConsumptions",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Requisitions_MaterialId",
                table: "Requisitions",
                column: "MaterialId");

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialBatchConsumptions_MaterialBatches_BatchId",
                table: "MaterialBatchConsumptions",
                column: "BatchId",
                principalTable: "MaterialBatches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialBatchConsumptions_users_CreatedById",
                table: "MaterialBatchConsumptions",
                column: "CreatedById",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialBatchConsumptions_users_LastDeletedById",
                table: "MaterialBatchConsumptions",
                column: "LastDeletedById",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialBatchConsumptions_users_LastUpdatedById",
                table: "MaterialBatchConsumptions",
                column: "LastUpdatedById",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialBatchConsumptions_users_UserId",
                table: "MaterialBatchConsumptions",
                column: "UserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Requisitions_Materials_MaterialId",
                table: "Requisitions",
                column: "MaterialId",
                principalTable: "Materials",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
