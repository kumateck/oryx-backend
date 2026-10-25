using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261025160000_AllowSharedBillingChargeReference")]
public partial class AllowSharedBillingChargeReference : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Payments_PayableType_PayableId_Reference",
            table: "Payments");
        migrationBuilder.CreateIndex(
            name: "IX_Payments_PayableType_PayableId_Reference",
            table: "Payments",
            columns: new[] { "PayableType", "PayableId", "Reference" },
            unique: true,
            filter: "\"PayableType\" <> 0");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Payments_PayableType_PayableId_Reference",
            table: "Payments");
        migrationBuilder.CreateIndex(
            name: "IX_Payments_PayableType_PayableId_Reference",
            table: "Payments",
            columns: new[] { "PayableType", "PayableId", "Reference" },
            unique: true);
    }
}
