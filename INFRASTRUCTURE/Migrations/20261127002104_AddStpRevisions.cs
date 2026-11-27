using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddStpRevisions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StpRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    StpNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RevisionNumber = table.Column<int>(type: "integer", nullable: false),
                    SupersedesRevision = table.Column<int>(type: "integer", nullable: true),
                    RevisedFromId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SupersededAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    ReasonForChange = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Area = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    EffectiveDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IssueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Source = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DeletionReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StpRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StpRevisions_StpRevisions_RevisedFromId",
                        column: x => x.RevisedFromId,
                        principalTable: "StpRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StpRevisions_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StpRevisions_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StpRevisions_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StpRevisionApprovals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StpRevisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovalId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: true),
                    Required = table.Column<bool>(type: "boolean", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    StageStartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ApprovalTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApprovedById = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActivatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Comments = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StpRevisionApprovals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StpRevisionApprovals_Approvals_ApprovalId",
                        column: x => x.ApprovalId,
                        principalTable: "Approvals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StpRevisionApprovals_StpRevisions_StpRevisionId",
                        column: x => x.StpRevisionId,
                        principalTable: "StpRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StpRevisionApprovals_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "roles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StpRevisionApprovals_users_ApprovedById",
                        column: x => x.ApprovedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StpRevisionApprovals_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StpRevisionSections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StpRevisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StpRevisionSections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StpRevisionSections_StpRevisions_StpRevisionId",
                        column: x => x.StpRevisionId,
                        principalTable: "StpRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StpRevisionSections_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StpRevisionSections_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StpRevisionSections_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StpRevisionTests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StpRevisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TestKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StpRevisionTests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StpRevisionTests_StpRevisions_StpRevisionId",
                        column: x => x.StpRevisionId,
                        principalTable: "StpRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StpRevisionTests_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StpRevisionTests_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StpRevisionTests_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StpRevisionTestSteps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StpRevisionTestId = table.Column<Guid>(type: "uuid", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    Number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Heading = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Text = table.Column<string>(type: "text", nullable: true),
                    ReferenceCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StpRevisionTestSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StpRevisionTestSteps_StpRevisionTests_StpRevisionTestId",
                        column: x => x.StpRevisionTestId,
                        principalTable: "StpRevisionTests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StpRevisionTestSteps_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StpRevisionTestSteps_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StpRevisionTestSteps_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_StpRevisionApprovals_ApprovalId",
                table: "StpRevisionApprovals",
                column: "ApprovalId");

            migrationBuilder.CreateIndex(
                name: "IX_StpRevisionApprovals_ApprovedById",
                table: "StpRevisionApprovals",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_StpRevisionApprovals_RoleId",
                table: "StpRevisionApprovals",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_StpRevisionApprovals_StpRevisionId",
                table: "StpRevisionApprovals",
                column: "StpRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_StpRevisionApprovals_UserId",
                table: "StpRevisionApprovals",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_StpRevisions_CreatedById",
                table: "StpRevisions",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_StpRevisions_Current",
                table: "StpRevisions",
                columns: new[] { "Kind", "StpNumber" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL AND \"Status\" = 1");

            migrationBuilder.CreateIndex(
                name: "IX_StpRevisions_InPreparation",
                table: "StpRevisions",
                columns: new[] { "Kind", "StpNumber" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL AND \"Status\" IN (0, 4)");

            migrationBuilder.CreateIndex(
                name: "IX_StpRevisions_LastDeletedById",
                table: "StpRevisions",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_StpRevisions_LastUpdatedById",
                table: "StpRevisions",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_StpRevisions_Number",
                table: "StpRevisions",
                columns: new[] { "Kind", "StpNumber", "RevisionNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_StpRevisions_RevisedFromId",
                table: "StpRevisions",
                column: "RevisedFromId");

            migrationBuilder.CreateIndex(
                name: "IX_StpRevisionSections_CreatedById",
                table: "StpRevisionSections",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_StpRevisionSections_LastDeletedById",
                table: "StpRevisionSections",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_StpRevisionSections_LastUpdatedById",
                table: "StpRevisionSections",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_StpRevisionSections_StpRevisionId_Kind",
                table: "StpRevisionSections",
                columns: new[] { "StpRevisionId", "Kind" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StpRevisionTests_CreatedById",
                table: "StpRevisionTests",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_StpRevisionTests_LastDeletedById",
                table: "StpRevisionTests",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_StpRevisionTests_LastUpdatedById",
                table: "StpRevisionTests",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_StpRevisionTests_StpRevisionId_TestKey",
                table: "StpRevisionTests",
                columns: new[] { "StpRevisionId", "TestKey" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StpRevisionTestSteps_CreatedById",
                table: "StpRevisionTestSteps",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_StpRevisionTestSteps_LastDeletedById",
                table: "StpRevisionTestSteps",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_StpRevisionTestSteps_LastUpdatedById",
                table: "StpRevisionTestSteps",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_StpRevisionTestSteps_StpRevisionTestId",
                table: "StpRevisionTestSteps",
                column: "StpRevisionTestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StpRevisionApprovals");

            migrationBuilder.DropTable(
                name: "StpRevisionSections");

            migrationBuilder.DropTable(
                name: "StpRevisionTestSteps");

            migrationBuilder.DropTable(
                name: "StpRevisionTests");

            migrationBuilder.DropTable(
                name: "StpRevisions");
        }
    }
}
