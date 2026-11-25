using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddApprovedVendorListDrafts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Draft and Pending share one preparation slot. The existing status index also protects Approved.
            migrationBuilder.Sql("""
                CREATE UNIQUE INDEX "IX_ApprovedVendorLists_Preparing"
                ON "ApprovedVendorLists" ((1))
                WHERE "DeletedAt" IS NULL AND "Status" IN (0, 4);
                """);
            migrationBuilder.AddColumn<string>(
                name: "DeletionReason",
                table: "ApprovedVendorLists",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DraftVersion",
                table: "ApprovedVendorLists",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<Guid>(
                name: "SupplierId",
                table: "ApprovedVendorListLines",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "ManufacturerId",
                table: "ApprovedVendorListLines",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "NewManufacturerAddress",
                table: "ApprovedVendorListLines",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NewManufacturerName",
                table: "ApprovedVendorListLines",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NewSupplierAddress",
                table: "ApprovedVendorListLines",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NewSupplierName",
                table: "ApprovedVendorListLines",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NewSupplierType",
                table: "ApprovedVendorListLines",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM "ApprovedVendorLists" WHERE "Status" = 4)
                       OR EXISTS (SELECT 1 FROM "ApprovedVendorListLines" WHERE "ManufacturerId" IS NULL OR "SupplierId" IS NULL) THEN
                        RAISE EXCEPTION 'Approved vendor list draft history must be preserved before reverting this migration.';
                    END IF;
                END $$;
                DROP INDEX "IX_ApprovedVendorLists_Preparing";
                """);
            migrationBuilder.DropColumn(
                name: "DeletionReason",
                table: "ApprovedVendorLists");

            migrationBuilder.DropColumn(
                name: "DraftVersion",
                table: "ApprovedVendorLists");

            migrationBuilder.DropColumn(
                name: "NewManufacturerAddress",
                table: "ApprovedVendorListLines");

            migrationBuilder.DropColumn(
                name: "NewManufacturerName",
                table: "ApprovedVendorListLines");

            migrationBuilder.DropColumn(
                name: "NewSupplierAddress",
                table: "ApprovedVendorListLines");

            migrationBuilder.DropColumn(
                name: "NewSupplierName",
                table: "ApprovedVendorListLines");

            migrationBuilder.DropColumn(
                name: "NewSupplierType",
                table: "ApprovedVendorListLines");

            migrationBuilder.AlterColumn<Guid>(
                name: "SupplierId",
                table: "ApprovedVendorListLines",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ManufacturerId",
                table: "ApprovedVendorListLines",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
