using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddQcCoaCertificates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QcCoas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TestRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    CertificateCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CertificateShape = table.Column<int>(type: "integer", nullable: false),
                    SupersedesId = table.Column<Guid>(type: "uuid", nullable: true),
                    RevisionNumber = table.Column<int>(type: "integer", nullable: false),
                    RevisionReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    IssuedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IssuedById = table.Column<Guid>(type: "uuid", nullable: true),
                    SpecificationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SpecificationVersion = table.Column<int>(type: "integer", nullable: false),
                    SpecificationCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ProductOrMaterialName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    BatchNumber = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    AreaOrRoom = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ManufacturingDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SampleDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TestCompletionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OverallComplies = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QcCoas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QcCoas_QcCoas_SupersedesId",
                        column: x => x.SupersedesId,
                        principalTable: "QcCoas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QcCoas_QcSpecifications_SpecificationId",
                        column: x => x.SpecificationId,
                        principalTable: "QcSpecifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QcCoas_QcTestRequests_TestRequestId",
                        column: x => x.TestRequestId,
                        principalTable: "QcTestRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QcCoas_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_QcCoas_users_IssuedById",
                        column: x => x.IssuedById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QcCoas_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_QcCoas_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "QcCoaRows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CoaId = table.Column<Guid>(type: "uuid", nullable: false),
                    TestRequestSubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectRef = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SubjectLabel = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SpecificationCharacteristicId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceWorksheetInstanceId = table.Column<Guid>(type: "uuid", nullable: true),
                    DisplayLabel = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    GroupName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    AcceptanceCriteria = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ResultValue = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Complies = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QcCoaRows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QcCoaRows_QcCoas_CoaId",
                        column: x => x.CoaId,
                        principalTable: "QcCoas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QcCoaRows_QcSpecificationCharacteristics_SpecificationChara~",
                        column: x => x.SpecificationCharacteristicId,
                        principalTable: "QcSpecificationCharacteristics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QcCoaRows_QcTestRequestSubjects_TestRequestSubjectId",
                        column: x => x.TestRequestSubjectId,
                        principalTable: "QcTestRequestSubjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QcCoaRows_QcWorksheetInstances_SourceWorksheetInstanceId",
                        column: x => x.SourceWorksheetInstanceId,
                        principalTable: "QcWorksheetInstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QcCoaRows_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_QcCoaRows_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_QcCoaRows_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_QcCoaRows_CoaId_TestRequestSubjectId_DisplayOrder",
                table: "QcCoaRows",
                columns: new[] { "CoaId", "TestRequestSubjectId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_QcCoaRows_CreatedById",
                table: "QcCoaRows",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcCoaRows_LastDeletedById",
                table: "QcCoaRows",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcCoaRows_LastUpdatedById",
                table: "QcCoaRows",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcCoaRows_SourceWorksheetInstanceId",
                table: "QcCoaRows",
                column: "SourceWorksheetInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_QcCoaRows_SpecificationCharacteristicId",
                table: "QcCoaRows",
                column: "SpecificationCharacteristicId");

            migrationBuilder.CreateIndex(
                name: "IX_QcCoaRows_TestRequestSubjectId",
                table: "QcCoaRows",
                column: "TestRequestSubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_QcCoas_CertificateCode",
                table: "QcCoas",
                column: "CertificateCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QcCoas_CreatedById",
                table: "QcCoas",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcCoas_IssuedAt",
                table: "QcCoas",
                column: "IssuedAt");

            migrationBuilder.CreateIndex(
                name: "IX_QcCoas_IssuedById",
                table: "QcCoas",
                column: "IssuedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcCoas_LastDeletedById",
                table: "QcCoas",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcCoas_LastUpdatedById",
                table: "QcCoas",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcCoas_SpecificationId",
                table: "QcCoas",
                column: "SpecificationId");

            migrationBuilder.CreateIndex(
                name: "IX_QcCoas_Status_CertificateShape",
                table: "QcCoas",
                columns: new[] { "Status", "CertificateShape" });

            migrationBuilder.CreateIndex(
                name: "IX_QcCoas_SupersedesId",
                table: "QcCoas",
                column: "SupersedesId");

            migrationBuilder.CreateIndex(
                name: "IX_QcCoas_TestRequestId_Status",
                table: "QcCoas",
                columns: new[] { "TestRequestId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QcCoaRows");

            migrationBuilder.DropTable(
                name: "QcCoas");
        }
    }
}
