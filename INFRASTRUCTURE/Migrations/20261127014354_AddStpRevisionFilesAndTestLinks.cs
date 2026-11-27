using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddStpRevisionFilesAndTestLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StpRevisionDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StpRevisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: true),
                    Sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Content = table.Column<byte[]>(type: "bytea", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StpRevisionDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StpRevisionDocuments_StpRevisions_StpRevisionId",
                        column: x => x.StpRevisionId,
                        principalTable: "StpRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StpRevisionDocuments_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StpRevisionDocuments_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StpRevisionDocuments_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StpTestLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FormSectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialSpecificationId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProductSpecificationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    StpNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TestKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StpTestLinks", x => x.Id);
                    table.CheckConstraint("CK_StpTestLinks_OneSpecification", "(\"MaterialSpecificationId\" IS NULL) <> (\"ProductSpecificationId\" IS NULL)");
                    table.ForeignKey(
                        name: "FK_StpTestLinks_FormSections_FormSectionId",
                        column: x => x.FormSectionId,
                        principalTable: "FormSections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StpTestLinks_MaterialSpecifications_MaterialSpecificationId",
                        column: x => x.MaterialSpecificationId,
                        principalTable: "MaterialSpecifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StpTestLinks_ProductSpecifications_ProductSpecificationId",
                        column: x => x.ProductSpecificationId,
                        principalTable: "ProductSpecifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StpTestLinks_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StpTestLinks_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StpTestLinks_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_StpRevisionDocuments_CreatedById",
                table: "StpRevisionDocuments",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_StpRevisionDocuments_LastDeletedById",
                table: "StpRevisionDocuments",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_StpRevisionDocuments_LastUpdatedById",
                table: "StpRevisionDocuments",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_StpRevisionDocuments_StpRevisionId",
                table: "StpRevisionDocuments",
                column: "StpRevisionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StpTestLinks_CreatedById",
                table: "StpTestLinks",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_StpTestLinks_FormSectionId",
                table: "StpTestLinks",
                column: "FormSectionId");

            migrationBuilder.CreateIndex(
                name: "IX_StpTestLinks_Kind_StpNumber_TestKey",
                table: "StpTestLinks",
                columns: new[] { "Kind", "StpNumber", "TestKey" });

            migrationBuilder.CreateIndex(
                name: "IX_StpTestLinks_LastDeletedById",
                table: "StpTestLinks",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_StpTestLinks_LastUpdatedById",
                table: "StpTestLinks",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_StpTestLinks_Material",
                table: "StpTestLinks",
                columns: new[] { "MaterialSpecificationId", "FormSectionId" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL AND \"MaterialSpecificationId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StpTestLinks_Product",
                table: "StpTestLinks",
                columns: new[] { "ProductSpecificationId", "FormSectionId" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL AND \"ProductSpecificationId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StpRevisionDocuments");

            migrationBuilder.DropTable(
                name: "StpTestLinks");
        }
    }
}
