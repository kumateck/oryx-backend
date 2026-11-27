using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddSpecificationMicrobialLimits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SpecificationMicrobialLimits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialSpecificationId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProductSpecificationId = table.Column<Guid>(type: "uuid", nullable: true),
                    FormFieldId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    Unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Organism = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    QuantityUnit = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SavedById = table.Column<Guid>(type: "uuid", nullable: false),
                    SavedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpecificationMicrobialLimits", x => x.Id);
                    table.CheckConstraint("CK_SpecificationMicrobialLimits_OneSpecification", "(\"MaterialSpecificationId\" IS NULL) <> (\"ProductSpecificationId\" IS NULL)");
                    table.ForeignKey(
                        name: "FK_SpecificationMicrobialLimits_FormFields_FormFieldId",
                        column: x => x.FormFieldId,
                        principalTable: "FormFields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SpecificationMicrobialLimits_MaterialSpecifications_Materia~",
                        column: x => x.MaterialSpecificationId,
                        principalTable: "MaterialSpecifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SpecificationMicrobialLimits_ProductSpecifications_ProductS~",
                        column: x => x.ProductSpecificationId,
                        principalTable: "ProductSpecifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SpecificationMicrobialLimits_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SpecificationMicrobialLimits_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SpecificationMicrobialLimits_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_SpecificationMicrobialLimits_CreatedById",
                table: "SpecificationMicrobialLimits",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_SpecificationMicrobialLimits_FormFieldId",
                table: "SpecificationMicrobialLimits",
                column: "FormFieldId");

            migrationBuilder.CreateIndex(
                name: "IX_SpecificationMicrobialLimits_LastDeletedById",
                table: "SpecificationMicrobialLimits",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_SpecificationMicrobialLimits_LastUpdatedById",
                table: "SpecificationMicrobialLimits",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_SpecificationMicrobialLimits_MaterialSpecificationId",
                table: "SpecificationMicrobialLimits",
                column: "MaterialSpecificationId");

            migrationBuilder.CreateIndex(
                name: "IX_SpecificationMicrobialLimits_ProductSpecificationId",
                table: "SpecificationMicrobialLimits",
                column: "ProductSpecificationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SpecificationMicrobialLimits");
        }
    }
}
