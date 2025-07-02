using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddSamplings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ProductSamplings: Convert SampleQuantity from text to numeric
            migrationBuilder.Sql("""
                ALTER TABLE "ProductSamplings"
                ALTER COLUMN "SampleQuantity" TYPE numeric
                USING "SampleQuantity"::numeric;
            """);

            migrationBuilder.Sql("""
                ALTER TABLE "ProductSamplings"
                ALTER COLUMN "SampleQuantity" SET NOT NULL;
            """);

            migrationBuilder.Sql("""
                ALTER TABLE "ProductSamplings"
                ALTER COLUMN "SampleQuantity" SET DEFAULT 0.0;
            """);

            // MaterialSamplings: Convert SampleQuantity from text to numeric
            migrationBuilder.Sql("""
                ALTER TABLE "MaterialSamplings"
                ALTER COLUMN "SampleQuantity" TYPE numeric
                USING "SampleQuantity"::numeric;
            """);

            migrationBuilder.Sql("""
                ALTER TABLE "MaterialSamplings"
                ALTER COLUMN "SampleQuantity" SET NOT NULL;
            """);

            migrationBuilder.Sql("""
                ALTER TABLE "MaterialSamplings"
                ALTER COLUMN "SampleQuantity" SET DEFAULT 0.0;
            """);

            // Add new decimal columns
            migrationBuilder.AddColumn<decimal>(
                name: "SampledQuantity",
                table: "MaterialBatches",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SampledQuantity",
                table: "BatchManufacturingRecords",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SampledQuantity",
                table: "MaterialBatches");

            migrationBuilder.DropColumn(
                name: "SampledQuantity",
                table: "BatchManufacturingRecords");

            // Revert SampleQuantity to text type for ProductSamplings
            migrationBuilder.AlterColumn<string>(
                name: "SampleQuantity",
                table: "ProductSamplings",
                type: "text",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            // Revert SampleQuantity to text type for MaterialSamplings
            migrationBuilder.AlterColumn<string>(
                name: "SampleQuantity",
                table: "MaterialSamplings",
                type: "text",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric");
        }
    }
}