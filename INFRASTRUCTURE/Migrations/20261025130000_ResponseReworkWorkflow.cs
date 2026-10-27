using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class ResponseReworkWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Responses_BatchManufacturingRecordId_ProductionActivityStep~",
                table: "Responses");

            migrationBuilder.AddColumn<int>(
                name: "RejectionOutcome",
                table: "Responses",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReworkSequence",
                table: "Responses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ResponseReworkTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceResponseId = table.Column<Guid>(type: "uuid", nullable: false),
                    RootResponseId = table.Column<Guid>(type: "uuid", nullable: false),
                    NewResponseId = table.Column<Guid>(type: "uuid", nullable: true),
                    MaterialBatchId = table.Column<Guid>(type: "uuid", nullable: true),
                    BatchManufacturingRecordId = table.Column<Guid>(type: "uuid", nullable: true),
                    Reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    RequestedById = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    StartedById = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("PK_ResponseReworkTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResponseReworkTasks_Responses_NewResponseId",
                        column: x => x.NewResponseId,
                        principalTable: "Responses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResponseReworkTasks_Responses_RootResponseId",
                        column: x => x.RootResponseId,
                        principalTable: "Responses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResponseReworkTasks_Responses_SourceResponseId",
                        column: x => x.SourceResponseId,
                        principalTable: "Responses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResponseReworkTasks_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ResponseReworkTasks_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ResponseReworkTasks_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Responses_BatchManufacturingRecordId_ProductionActivityStep~",
                table: "Responses",
                columns: new[] { "BatchManufacturingRecordId", "ProductionActivityStepId", "ReworkSequence" },
                unique: true,
                filter: "\"BatchManufacturingRecordId\" IS NOT NULL AND \"ProductionActivityStepId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ResponseReworkTasks_CreatedById",
                table: "ResponseReworkTasks",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ResponseReworkTasks_LastDeletedById",
                table: "ResponseReworkTasks",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_ResponseReworkTasks_LastUpdatedById",
                table: "ResponseReworkTasks",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ResponseReworkTasks_NewResponseId",
                table: "ResponseReworkTasks",
                column: "NewResponseId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResponseReworkTasks_RootResponseId",
                table: "ResponseReworkTasks",
                column: "RootResponseId");

            migrationBuilder.CreateIndex(
                name: "IX_ResponseReworkTasks_SourceResponseId",
                table: "ResponseReworkTasks",
                column: "SourceResponseId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ResponseReworkTasks");

            migrationBuilder.DropIndex(
                name: "IX_Responses_BatchManufacturingRecordId_ProductionActivityStep~",
                table: "Responses");

            migrationBuilder.DropColumn(
                name: "RejectionOutcome",
                table: "Responses");

            migrationBuilder.DropColumn(
                name: "ReworkSequence",
                table: "Responses");

            migrationBuilder.CreateIndex(
                name: "IX_Responses_BatchManufacturingRecordId_ProductionActivityStep~",
                table: "Responses",
                columns: new[] { "BatchManufacturingRecordId", "ProductionActivityStepId" },
                unique: true,
                filter: "\"BatchManufacturingRecordId\" IS NOT NULL AND \"ProductionActivityStepId\" IS NOT NULL");
        }
    }
}
