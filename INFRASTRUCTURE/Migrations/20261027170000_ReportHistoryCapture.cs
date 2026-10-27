using INFRASTRUCTURE.Context;
using INFRASTRUCTURE.Reporting;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace INFRASTRUCTURE.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261027170000_ReportHistoryCapture")]
public class ReportHistoryCapture : Migration
{
    // Operational append-only history is intentionally managed by SQL, not application entity writes.
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql(ReportHistorySchema.InstallSql);
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // An application rollback must not erase audit evidence or create a capture gap.
        // The additive history tables/triggers remain compatible with older applications.
    }
}
