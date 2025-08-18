using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddMaterialSpecsandProductSpecs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TestSpecification");

            migrationBuilder.AddColumn<DateTime>(
                name: "EffectiveDate",
                table: "ProductSpecifications",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewDate",
                table: "ProductSpecifications",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "RevisionNumber",
                table: "ProductSpecifications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SpecificationNumber",
                table: "ProductSpecifications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupersedesNumber",
                table: "ProductSpecifications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TestStage",
                table: "ProductSpecifications",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "MaterialSpecifications_TestSpecifications",
                columns: table => new
                {
                    MaterialSpecificationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SrNumber = table.Column<int>(type: "integer", nullable: false),
                    TestName = table.Column<int>(type: "integer", nullable: false),
                    ReleaseSpecification = table.Column<string>(type: "text", nullable: true),
                    Reference = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialSpecifications_TestSpecifications", x => new { x.MaterialSpecificationId, x.Id });
                    table.ForeignKey(
                        name: "FK_MaterialSpecifications_TestSpecifications_MaterialSpecifica~",
                        column: x => x.MaterialSpecificationId,
                        principalTable: "MaterialSpecifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductSpecifications_TestSpecifications",
                columns: table => new
                {
                    ProductSpecificationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SrNumber = table.Column<int>(type: "integer", nullable: false),
                    TestName = table.Column<int>(type: "integer", nullable: false),
                    ReleaseSpecification = table.Column<string>(type: "text", nullable: true),
                    Reference = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductSpecifications_TestSpecifications", x => new { x.ProductSpecificationId, x.Id });
                    table.ForeignKey(
                        name: "FK_ProductSpecifications_TestSpecifications_ProductSpecificati~",
                        column: x => x.ProductSpecificationId,
                        principalTable: "ProductSpecifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MaterialSpecifications_TestSpecifications");

            migrationBuilder.DropTable(
                name: "ProductSpecifications_TestSpecifications");

            migrationBuilder.DropColumn(
                name: "EffectiveDate",
                table: "ProductSpecifications");

            migrationBuilder.DropColumn(
                name: "ReviewDate",
                table: "ProductSpecifications");

            migrationBuilder.DropColumn(
                name: "RevisionNumber",
                table: "ProductSpecifications");

            migrationBuilder.DropColumn(
                name: "SpecificationNumber",
                table: "ProductSpecifications");

            migrationBuilder.DropColumn(
                name: "SupersedesNumber",
                table: "ProductSpecifications");

            migrationBuilder.DropColumn(
                name: "TestStage",
                table: "ProductSpecifications");

            migrationBuilder.CreateTable(
                name: "TestSpecification",
                columns: table => new
                {
                    MaterialSpecificationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Reference = table.Column<int>(type: "integer", nullable: false),
                    ReleaseSpecification = table.Column<string>(type: "text", nullable: true),
                    SrNumber = table.Column<int>(type: "integer", nullable: false),
                    TestName = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestSpecification", x => new { x.MaterialSpecificationId, x.Id });
                    table.ForeignKey(
                        name: "FK_TestSpecification_MaterialSpecifications_MaterialSpecificat~",
                        column: x => x.MaterialSpecificationId,
                        principalTable: "MaterialSpecifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }
    }
}
