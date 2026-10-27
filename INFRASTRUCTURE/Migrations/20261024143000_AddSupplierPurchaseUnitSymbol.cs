using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace INFRASTRUCTURE.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261024143000_AddSupplierPurchaseUnitSymbol")]
public sealed class AddSupplierPurchaseUnitSymbol : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "PurchaseUnit", table: "SupplierManufacturers",
            type: "character varying(20)", maxLength: 20, nullable: true);
        migrationBuilder.Sql("""
            UPDATE "SupplierManufacturers" AS association
            SET "PurchaseUnit" = CASE lower(trim(unit."Symbol"))
                WHEN 'l' THEN 'L' WHEN 'ml' THEN 'ml' WHEN 'kg' THEN 'kg'
                WHEN 'g' THEN 'g' WHEN 'mg' THEN 'mg' ELSE trim(unit."Symbol") END
            FROM "UnitOfMeasures" AS unit
            WHERE association."UoMId" = unit."Id" AND association."PurchaseUnit" IS NULL
              AND nullif(trim(unit."Symbol"), '') IS NOT NULL;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropColumn(name: "PurchaseUnit", table: "SupplierManufacturers");
}
