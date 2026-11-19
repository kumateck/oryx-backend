using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddApprovedVendorLists : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApprovedVendorLists",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AnnexureNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RevisionNumber = table.Column<int>(type: "integer", nullable: false),
                    SupersedesRevision = table.Column<int>(type: "integer", nullable: true),
                    PreviousRevisionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SupersededAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovedVendorLists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovedVendorLists_ApprovedVendorLists_PreviousRevisionId",
                        column: x => x.PreviousRevisionId,
                        principalTable: "ApprovedVendorLists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ApprovedVendorLists_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ApprovedVendorLists_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ApprovedVendorLists_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ApprovedVendorListApprovals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovedVendorListId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_ApprovedVendorListApprovals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovedVendorListApprovals_Approvals_ApprovalId",
                        column: x => x.ApprovalId,
                        principalTable: "Approvals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ApprovedVendorListApprovals_ApprovedVendorLists_ApprovedVen~",
                        column: x => x.ApprovedVendorListId,
                        principalTable: "ApprovedVendorLists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApprovedVendorListApprovals_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "roles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ApprovedVendorListApprovals_users_ApprovedById",
                        column: x => x.ApprovedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ApprovedVendorListApprovals_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ApprovedVendorListLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovedVendorListId = table.Column<Guid>(type: "uuid", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    ManufacturerId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuantityPerPack = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    ApprovalDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ValidityDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovedVendorListLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovedVendorListLines_ApprovedVendorLists_ApprovedVendorL~",
                        column: x => x.ApprovedVendorListId,
                        principalTable: "ApprovedVendorLists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApprovedVendorListLines_Manufacturers_ManufacturerId",
                        column: x => x.ManufacturerId,
                        principalTable: "Manufacturers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ApprovedVendorListLines_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ApprovedVendorListLines_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ApprovedVendorListLines_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ApprovedVendorListLines_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ApprovedVendorListLines_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovedVendorListApprovals_ApprovalId",
                table: "ApprovedVendorListApprovals",
                column: "ApprovalId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovedVendorListApprovals_ApprovedById",
                table: "ApprovedVendorListApprovals",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovedVendorListApprovals_ApprovedVendorListId",
                table: "ApprovedVendorListApprovals",
                column: "ApprovedVendorListId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovedVendorListApprovals_RoleId",
                table: "ApprovedVendorListApprovals",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovedVendorListApprovals_UserId",
                table: "ApprovedVendorListApprovals",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovedVendorListLines_ApprovedVendorListId_MaterialId_Man~",
                table: "ApprovedVendorListLines",
                columns: new[] { "ApprovedVendorListId", "MaterialId", "ManufacturerId", "SupplierId" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovedVendorListLines_CreatedById",
                table: "ApprovedVendorListLines",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovedVendorListLines_LastDeletedById",
                table: "ApprovedVendorListLines",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovedVendorListLines_LastUpdatedById",
                table: "ApprovedVendorListLines",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovedVendorListLines_ManufacturerId",
                table: "ApprovedVendorListLines",
                column: "ManufacturerId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovedVendorListLines_MaterialId",
                table: "ApprovedVendorListLines",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovedVendorListLines_SupplierId",
                table: "ApprovedVendorListLines",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovedVendorLists_CreatedById",
                table: "ApprovedVendorLists",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovedVendorLists_LastDeletedById",
                table: "ApprovedVendorLists",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovedVendorLists_LastUpdatedById",
                table: "ApprovedVendorLists",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovedVendorLists_OpenStatus",
                table: "ApprovedVendorLists",
                column: "Status",
                unique: true,
                filter: "\"DeletedAt\" IS NULL AND \"Status\" IN (0, 1)");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovedVendorLists_PreviousRevisionId",
                table: "ApprovedVendorLists",
                column: "PreviousRevisionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApprovedVendorListApprovals");

            migrationBuilder.DropTable(
                name: "ApprovedVendorListLines");

            migrationBuilder.DropTable(
                name: "ApprovedVendorLists");
        }
    }
}
