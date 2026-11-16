using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentReviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DocumentReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ModelType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ConfigurationType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ModelId = table.Column<Guid>(type: "uuid", nullable: false),
                    Submission = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PreparedById = table.Column<Guid>(type: "uuid", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReviewedById = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Comments = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ApprovalsStartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentReviews_users_PreparedById",
                        column: x => x.PreparedById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentReviews_users_ReviewedById",
                        column: x => x.ReviewedById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReviewConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReviewConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReviewConfigurations_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ReviewConfigurations_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ReviewConfigurations_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DocumentReviewEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentReviewId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<int>(type: "integer", nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: true),
                    Comments = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    At = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentReviewEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentReviewEvents_DocumentReviews_DocumentReviewId",
                        column: x => x.DocumentReviewId,
                        principalTable: "DocumentReviews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentReviewEvents_users_ActorId",
                        column: x => x.ActorId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReviewConfigurationAssignees",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewConfigurationId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReviewConfigurationAssignees", x => x.Id);
                    table.CheckConstraint("CK_ReviewConfigurationAssignees_OneAssignee", "(\"RoleId\" IS NULL) <> (\"UserId\" IS NULL)");
                    table.ForeignKey(
                        name: "FK_ReviewConfigurationAssignees_ReviewConfigurations_ReviewCon~",
                        column: x => x.ReviewConfigurationId,
                        principalTable: "ReviewConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReviewConfigurationAssignees_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReviewConfigurationAssignees_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentReviewEvents_ActorId",
                table: "DocumentReviewEvents",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentReviewEvents_DocumentReviewId",
                table: "DocumentReviewEvents",
                column: "DocumentReviewId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentReviews_ModelType_ModelId_Submission",
                table: "DocumentReviews",
                columns: new[] { "ModelType", "ModelId", "Submission" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentReviews_Open",
                table: "DocumentReviews",
                columns: new[] { "ModelType", "ModelId" },
                unique: true,
                filter: "\"Status\" IN (0, 1)");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentReviews_PreparedById",
                table: "DocumentReviews",
                column: "PreparedById");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentReviews_ReviewedById",
                table: "DocumentReviews",
                column: "ReviewedById");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentReviews_Status_ConfigurationType",
                table: "DocumentReviews",
                columns: new[] { "Status", "ConfigurationType" });

            migrationBuilder.CreateIndex(
                name: "IX_ReviewConfigurationAssignees_ReviewConfigurationId",
                table: "ReviewConfigurationAssignees",
                column: "ReviewConfigurationId");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewConfigurationAssignees_RoleId",
                table: "ReviewConfigurationAssignees",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewConfigurationAssignees_UserId",
                table: "ReviewConfigurationAssignees",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewConfigurations_CreatedById",
                table: "ReviewConfigurations",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewConfigurations_ItemType",
                table: "ReviewConfigurations",
                column: "ItemType",
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewConfigurations_LastDeletedById",
                table: "ReviewConfigurations",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewConfigurations_LastUpdatedById",
                table: "ReviewConfigurations",
                column: "LastUpdatedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentReviewEvents");

            migrationBuilder.DropTable(
                name: "ReviewConfigurationAssignees");

            migrationBuilder.DropTable(
                name: "DocumentReviews");

            migrationBuilder.DropTable(
                name: "ReviewConfigurations");
        }
    }
}
