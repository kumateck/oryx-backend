using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class UpdateFormReviewer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FormAssignees_users_UserId",
                table: "FormAssignees");

            migrationBuilder.DropIndex(
                name: "IX_FormAssignees_UserId",
                table: "FormAssignees");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "FormAssignees");

            migrationBuilder.AddColumn<Guid>(
                name: "BatchManufacturingRecordId",
                table: "FormAssignees",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "FormAssignees",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedById",
                table: "FormAssignees",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "FormAssignees",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LastDeletedById",
                table: "FormAssignees",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LastUpdatedById",
                table: "FormAssignees",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MaterialBatchId",
                table: "FormAssignees",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProductionActivityStepId",
                table: "FormAssignees",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "FormAssignees",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FormFieldAssignees",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FormAssigneeId = table.Column<Guid>(type: "uuid", nullable: false),
                    FormFieldId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssigneeId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormFieldAssignees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FormFieldAssignees_FormAssignees_FormAssigneeId",
                        column: x => x.FormAssigneeId,
                        principalTable: "FormAssignees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FormFieldAssignees_FormFields_FormFieldId",
                        column: x => x.FormFieldId,
                        principalTable: "FormFields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FormFieldAssignees_users_AssigneeId",
                        column: x => x.AssigneeId,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FormFieldAssignees_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FormFieldAssignees_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FormFieldAssignees_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_FormAssignees_BatchManufacturingRecordId",
                table: "FormAssignees",
                column: "BatchManufacturingRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_FormAssignees_CreatedById",
                table: "FormAssignees",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_FormAssignees_LastDeletedById",
                table: "FormAssignees",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_FormAssignees_LastUpdatedById",
                table: "FormAssignees",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_FormAssignees_MaterialBatchId",
                table: "FormAssignees",
                column: "MaterialBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_FormAssignees_ProductionActivityStepId",
                table: "FormAssignees",
                column: "ProductionActivityStepId");

            migrationBuilder.CreateIndex(
                name: "IX_FormFieldAssignees_AssigneeId",
                table: "FormFieldAssignees",
                column: "AssigneeId");

            migrationBuilder.CreateIndex(
                name: "IX_FormFieldAssignees_CreatedById",
                table: "FormFieldAssignees",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_FormFieldAssignees_FormAssigneeId",
                table: "FormFieldAssignees",
                column: "FormAssigneeId");

            migrationBuilder.CreateIndex(
                name: "IX_FormFieldAssignees_FormFieldId",
                table: "FormFieldAssignees",
                column: "FormFieldId");

            migrationBuilder.CreateIndex(
                name: "IX_FormFieldAssignees_LastDeletedById",
                table: "FormFieldAssignees",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_FormFieldAssignees_LastUpdatedById",
                table: "FormFieldAssignees",
                column: "LastUpdatedById");

            migrationBuilder.AddForeignKey(
                name: "FK_FormAssignees_BatchManufacturingRecords_BatchManufacturingR~",
                table: "FormAssignees",
                column: "BatchManufacturingRecordId",
                principalTable: "BatchManufacturingRecords",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_FormAssignees_MaterialBatches_MaterialBatchId",
                table: "FormAssignees",
                column: "MaterialBatchId",
                principalTable: "MaterialBatches",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_FormAssignees_ProductionActivitySteps_ProductionActivitySte~",
                table: "FormAssignees",
                column: "ProductionActivityStepId",
                principalTable: "ProductionActivitySteps",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_FormAssignees_users_CreatedById",
                table: "FormAssignees",
                column: "CreatedById",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_FormAssignees_users_LastDeletedById",
                table: "FormAssignees",
                column: "LastDeletedById",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_FormAssignees_users_LastUpdatedById",
                table: "FormAssignees",
                column: "LastUpdatedById",
                principalTable: "users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FormAssignees_BatchManufacturingRecords_BatchManufacturingR~",
                table: "FormAssignees");

            migrationBuilder.DropForeignKey(
                name: "FK_FormAssignees_MaterialBatches_MaterialBatchId",
                table: "FormAssignees");

            migrationBuilder.DropForeignKey(
                name: "FK_FormAssignees_ProductionActivitySteps_ProductionActivitySte~",
                table: "FormAssignees");

            migrationBuilder.DropForeignKey(
                name: "FK_FormAssignees_users_CreatedById",
                table: "FormAssignees");

            migrationBuilder.DropForeignKey(
                name: "FK_FormAssignees_users_LastDeletedById",
                table: "FormAssignees");

            migrationBuilder.DropForeignKey(
                name: "FK_FormAssignees_users_LastUpdatedById",
                table: "FormAssignees");

            migrationBuilder.DropTable(
                name: "FormFieldAssignees");

            migrationBuilder.DropIndex(
                name: "IX_FormAssignees_BatchManufacturingRecordId",
                table: "FormAssignees");

            migrationBuilder.DropIndex(
                name: "IX_FormAssignees_CreatedById",
                table: "FormAssignees");

            migrationBuilder.DropIndex(
                name: "IX_FormAssignees_LastDeletedById",
                table: "FormAssignees");

            migrationBuilder.DropIndex(
                name: "IX_FormAssignees_LastUpdatedById",
                table: "FormAssignees");

            migrationBuilder.DropIndex(
                name: "IX_FormAssignees_MaterialBatchId",
                table: "FormAssignees");

            migrationBuilder.DropIndex(
                name: "IX_FormAssignees_ProductionActivityStepId",
                table: "FormAssignees");

            migrationBuilder.DropColumn(
                name: "BatchManufacturingRecordId",
                table: "FormAssignees");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "FormAssignees");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "FormAssignees");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "FormAssignees");

            migrationBuilder.DropColumn(
                name: "LastDeletedById",
                table: "FormAssignees");

            migrationBuilder.DropColumn(
                name: "LastUpdatedById",
                table: "FormAssignees");

            migrationBuilder.DropColumn(
                name: "MaterialBatchId",
                table: "FormAssignees");

            migrationBuilder.DropColumn(
                name: "ProductionActivityStepId",
                table: "FormAssignees");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "FormAssignees");

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "FormAssignees",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_FormAssignees_UserId",
                table: "FormAssignees",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_FormAssignees_users_UserId",
                table: "FormAssignees",
                column: "UserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
