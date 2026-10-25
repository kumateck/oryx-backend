using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class MaterialRetestWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MaterialRetestRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialBatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceKey = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    Origin = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PreviousBatchStatus = table.Column<int>(type: "integer", nullable: false),
                    Approved = table.Column<bool>(type: "boolean", nullable: false),
                    ScheduledFor = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OriginalExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    SampleTakenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SampleTakenById = table.Column<Guid>(type: "uuid", nullable: true),
                    SampleQuantity = table.Column<decimal>(type: "numeric", nullable: true),
                    SampledContainers = table.Column<int>(type: "integer", nullable: true),
                    ProposedExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AcknowledgedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AcknowledgedById = table.Column<Guid>(type: "uuid", nullable: true),
                    TestRequestId = table.Column<Guid>(type: "uuid", nullable: true),
                    CoaId = table.Column<Guid>(type: "uuid", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedById = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialRetestRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaterialRetestRequests_MaterialBatches_MaterialBatchId",
                        column: x => x.MaterialBatchId,
                        principalTable: "MaterialBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaterialRetestRequests_QcCoas_CoaId",
                        column: x => x.CoaId,
                        principalTable: "QcCoas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaterialRetestRequests_QcTestRequests_TestRequestId",
                        column: x => x.TestRequestId,
                        principalTable: "QcTestRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaterialRetestRequests_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MaterialRetestRequests_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MaterialRetestRequests_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MaterialRetestEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialRetestRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Detail = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialRetestEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaterialRetestEvents_MaterialRetestRequests_MaterialRetestR~",
                        column: x => x.MaterialRetestRequestId,
                        principalTable: "MaterialRetestRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MaterialRetestEvents_MaterialRetestRequestId",
                table: "MaterialRetestEvents",
                column: "MaterialRetestRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialRetestRequests_CoaId",
                table: "MaterialRetestRequests",
                column: "CoaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaterialRetestRequests_CreatedById",
                table: "MaterialRetestRequests",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialRetestRequests_LastDeletedById",
                table: "MaterialRetestRequests",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialRetestRequests_LastUpdatedById",
                table: "MaterialRetestRequests",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialRetestRequests_MaterialBatchId",
                table: "MaterialRetestRequests",
                column: "MaterialBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialRetestRequests_SourceKey",
                table: "MaterialRetestRequests",
                column: "SourceKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaterialRetestRequests_TestRequestId",
                table: "MaterialRetestRequests",
                column: "TestRequestId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MaterialRetestEvents");

            migrationBuilder.DropTable(
                name: "MaterialRetestRequests");
        }
    }
}
