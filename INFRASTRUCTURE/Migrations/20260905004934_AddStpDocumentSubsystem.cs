using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddStpDocumentSubsystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StpDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerType = table.Column<string>(type: "text", nullable: true),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CurrentDraftVersionId = table.Column<Guid>(type: "uuid", nullable: true),
                    EffectiveVersionId = table.Column<Guid>(type: "uuid", nullable: true),
                    LockedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LockedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StpDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StpDocuments_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StpDocuments_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StpDocuments_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StpDocuments_users_LockedById",
                        column: x => x.LockedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StpDocumentVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StpDocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionNumber = table.Column<int>(type: "integer", nullable: false),
                    StorageKey = table.Column<string>(type: "text", nullable: true),
                    FileName = table.Column<string>(type: "text", nullable: true),
                    Sha256 = table.Column<string>(type: "text", nullable: true),
                    Size = table.Column<long>(type: "bigint", nullable: false),
                    Source = table.Column<int>(type: "integer", nullable: false),
                    ReasonForChange = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StpDocumentVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StpDocumentVersions_StpDocuments_StpDocumentId",
                        column: x => x.StpDocumentId,
                        principalTable: "StpDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StpDocumentVersions_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StpDocumentVersions_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StpDocumentVersions_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StpDocumentSignatures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StpDocumentVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<int>(type: "integer", nullable: false),
                    Meaning = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StpDocumentSignatures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StpDocumentSignatures_StpDocumentVersions_StpDocumentVersio~",
                        column: x => x.StpDocumentVersionId,
                        principalTable: "StpDocumentVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StpDocumentSignatures_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StpDocumentSignatures_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StpDocumentSignatures_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_StpDocuments_CreatedById",
                table: "StpDocuments",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_StpDocuments_CurrentDraftVersionId",
                table: "StpDocuments",
                column: "CurrentDraftVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_StpDocuments_EffectiveVersionId",
                table: "StpDocuments",
                column: "EffectiveVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_StpDocuments_LastDeletedById",
                table: "StpDocuments",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_StpDocuments_LastUpdatedById",
                table: "StpDocuments",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_StpDocuments_LockedById",
                table: "StpDocuments",
                column: "LockedById");

            migrationBuilder.CreateIndex(
                name: "IX_StpDocuments_OwnerType_OwnerId",
                table: "StpDocuments",
                columns: new[] { "OwnerType", "OwnerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StpDocumentSignatures_CreatedById",
                table: "StpDocumentSignatures",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_StpDocumentSignatures_LastDeletedById",
                table: "StpDocumentSignatures",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_StpDocumentSignatures_LastUpdatedById",
                table: "StpDocumentSignatures",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_StpDocumentSignatures_StpDocumentVersionId",
                table: "StpDocumentSignatures",
                column: "StpDocumentVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_StpDocumentVersions_CreatedById",
                table: "StpDocumentVersions",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_StpDocumentVersions_LastDeletedById",
                table: "StpDocumentVersions",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_StpDocumentVersions_LastUpdatedById",
                table: "StpDocumentVersions",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_StpDocumentVersions_StpDocumentId",
                table: "StpDocumentVersions",
                column: "StpDocumentId");

            migrationBuilder.AddForeignKey(
                name: "FK_StpDocuments_StpDocumentVersions_CurrentDraftVersionId",
                table: "StpDocuments",
                column: "CurrentDraftVersionId",
                principalTable: "StpDocumentVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StpDocuments_StpDocumentVersions_EffectiveVersionId",
                table: "StpDocuments",
                column: "EffectiveVersionId",
                principalTable: "StpDocumentVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StpDocuments_StpDocumentVersions_CurrentDraftVersionId",
                table: "StpDocuments");

            migrationBuilder.DropForeignKey(
                name: "FK_StpDocuments_StpDocumentVersions_EffectiveVersionId",
                table: "StpDocuments");

            migrationBuilder.DropTable(
                name: "StpDocumentSignatures");

            migrationBuilder.DropTable(
                name: "StpDocumentVersions");

            migrationBuilder.DropTable(
                name: "StpDocuments");
        }
    }
}
