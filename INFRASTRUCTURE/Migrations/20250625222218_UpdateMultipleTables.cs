using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class UpdateMultipleTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaterialAnalyticalRawData_MaterialBatches_MaterialBatchId",
                table: "MaterialAnalyticalRawData");

            migrationBuilder.DropForeignKey(
                name: "FK_Responses_MaterialAnalyticalRawData_MaterialAnalyticalRawDa~",
                table: "Responses");

            migrationBuilder.DropIndex(
                name: "IX_MaterialAnalyticalRawData_MaterialBatchId",
                table: "MaterialAnalyticalRawData");

            migrationBuilder.DropColumn(
                name: "MaterialBatchId",
                table: "MaterialAnalyticalRawData");

            migrationBuilder.DropColumn(
                name: "State",
                table: "AnalyticalTestRequests");

            migrationBuilder.RenameColumn(
                name: "MaterialAnalyticalRawDataId",
                table: "Responses",
                newName: "MaterialBatchId");

            migrationBuilder.RenameIndex(
                name: "IX_Responses_MaterialAnalyticalRawDataId",
                table: "Responses",
                newName: "IX_Responses_MaterialBatchId");

            migrationBuilder.AddColumn<Guid>(
                name: "BatchManufacturingRecordId",
                table: "Responses",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "BatchManufacturingRecords",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "StateId",
                table: "AnalyticalTestRequests",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "ProductStates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductStates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductStates_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductStates_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductStates_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Responses_BatchManufacturingRecordId",
                table: "Responses",
                column: "BatchManufacturingRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticalTestRequests_StateId",
                table: "AnalyticalTestRequests",
                column: "StateId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductStates_CreatedById",
                table: "ProductStates",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductStates_LastDeletedById",
                table: "ProductStates",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductStates_LastUpdatedById",
                table: "ProductStates",
                column: "LastUpdatedById");

            migrationBuilder.AddForeignKey(
                name: "FK_AnalyticalTestRequests_ProductStates_StateId",
                table: "AnalyticalTestRequests",
                column: "StateId",
                principalTable: "ProductStates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Responses_BatchManufacturingRecords_BatchManufacturingRecor~",
                table: "Responses",
                column: "BatchManufacturingRecordId",
                principalTable: "BatchManufacturingRecords",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Responses_MaterialBatches_MaterialBatchId",
                table: "Responses",
                column: "MaterialBatchId",
                principalTable: "MaterialBatches",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AnalyticalTestRequests_ProductStates_StateId",
                table: "AnalyticalTestRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_Responses_BatchManufacturingRecords_BatchManufacturingRecor~",
                table: "Responses");

            migrationBuilder.DropForeignKey(
                name: "FK_Responses_MaterialBatches_MaterialBatchId",
                table: "Responses");

            migrationBuilder.DropTable(
                name: "ProductStates");

            migrationBuilder.DropIndex(
                name: "IX_Responses_BatchManufacturingRecordId",
                table: "Responses");

            migrationBuilder.DropIndex(
                name: "IX_AnalyticalTestRequests_StateId",
                table: "AnalyticalTestRequests");

            migrationBuilder.DropColumn(
                name: "BatchManufacturingRecordId",
                table: "Responses");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "BatchManufacturingRecords");

            migrationBuilder.DropColumn(
                name: "StateId",
                table: "AnalyticalTestRequests");

            migrationBuilder.RenameColumn(
                name: "MaterialBatchId",
                table: "Responses",
                newName: "MaterialAnalyticalRawDataId");

            migrationBuilder.RenameIndex(
                name: "IX_Responses_MaterialBatchId",
                table: "Responses",
                newName: "IX_Responses_MaterialAnalyticalRawDataId");

            migrationBuilder.AddColumn<Guid>(
                name: "MaterialBatchId",
                table: "MaterialAnalyticalRawData",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "State",
                table: "AnalyticalTestRequests",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_MaterialAnalyticalRawData_MaterialBatchId",
                table: "MaterialAnalyticalRawData",
                column: "MaterialBatchId");

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialAnalyticalRawData_MaterialBatches_MaterialBatchId",
                table: "MaterialAnalyticalRawData",
                column: "MaterialBatchId",
                principalTable: "MaterialBatches",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Responses_MaterialAnalyticalRawData_MaterialAnalyticalRawDa~",
                table: "Responses",
                column: "MaterialAnalyticalRawDataId",
                principalTable: "MaterialAnalyticalRawData",
                principalColumn: "Id");
        }
    }
}
