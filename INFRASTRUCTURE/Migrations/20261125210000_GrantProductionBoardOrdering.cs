using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace INFRASTRUCTURE.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261125210000_GrantProductionBoardOrdering")]
public sealed class GrantProductionBoardOrdering : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(GrantProductSectionPermissions.Grant("CanViewDepartmentProductionBoardOrder", "CanViewOperations"));
        migrationBuilder.Sql(GrantProductSectionPermissions.Grant("CanReorderDepartmentProductionBoard", "CanEditOperation"));
    }
    protected override void Down(MigrationBuilder migrationBuilder) { }
}
