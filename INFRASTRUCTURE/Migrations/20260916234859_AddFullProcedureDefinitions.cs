using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations;

public partial class AddFullProcedureDefinitions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        CreateCoreTables(migrationBuilder);
        CreateDetailTables(migrationBuilder);
        CreateIndexes(migrationBuilder);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ProcedureApplicabilities");
        migrationBuilder.DropTable(name: "ProcedureRevisionAudits");
        migrationBuilder.DropTable(name: "ProcedureStageScopes");
        migrationBuilder.DropTable(name: "ProcedureRevisions");
        migrationBuilder.DropTable(name: "ProcedureDefinitions");
    }
}
