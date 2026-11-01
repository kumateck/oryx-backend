using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations;

/// <summary>
/// Production runs started before roles were scoped by department list every holder of a
/// production role (for example every General Manager Production) as responsible, whatever their
/// department. On unfinished steps of unfinished runs, this hides people who sit in a different
/// production department than the product, but only where someone who stays still covers the
/// same action, so no step is left without anyone able to carry it out. The rows are marked with
/// a fixed deletion time so Down can restore exactly these.
/// </summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20261031090000_ScopeProductionStepAssigneesToDepartment")]
public partial class ScopeProductionStepAssigneesToDepartment : Migration
{
    private const string Marker = "TIMESTAMPTZ '2026-10-31 09:00:00.000777+00'";

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // ProductionStatus: 2 = Completed, 4 = Cancelled. DepartmentType: 0 = Production.
        migrationBuilder.Sql($"""
            UPDATE "ProductionActivityStepUsers" AS su
            SET "DeletedAt" = {Marker}
            FROM "ProductionActivitySteps" AS s
            JOIN "ProductionActivities" AS a ON a."Id" = s."ProductionActivityId"
            JOIN "ProductionScheduleProducts" AS psp ON psp."Id" = a."ProductionScheduleProductId"
            JOIN "Products" AS p ON p."Id" = psp."ProductId"
            WHERE s."Id" = su."ProductionActivityStepId"
              AND su."DeletedAt" IS NULL
              AND s."Status" NOT IN (2, 4)
              AND a."Status" NOT IN (2, 4)
              AND p."DepartmentId" IS NOT NULL
              AND EXISTS (
                  SELECT 1 FROM users AS u
                  JOIN "Departments" AS d ON d."Id" = u."DepartmentId"
                  WHERE u."Id" = su."UserId" AND d."Type" = 0 AND u."DepartmentId" <> p."DepartmentId")
              AND EXISTS (
                  SELECT 1 FROM "ProductionActivityStepUsers" AS keep
                  JOIN users AS ku ON ku."Id" = keep."UserId"
                  LEFT JOIN "Departments" AS kd ON kd."Id" = ku."DepartmentId"
                  WHERE keep."ProductionActivityStepId" = su."ProductionActivityStepId"
                    AND keep."Action" = su."Action"
                    AND keep."DeletedAt" IS NULL
                    AND ku."DeletedAt" IS NULL
                    AND (ku."DepartmentId" IS NULL OR ku."DepartmentId" = p."DepartmentId" OR kd."Type" IS DISTINCT FROM 0));
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql($"""
            UPDATE "ProductionActivityStepUsers" SET "DeletedAt" = NULL WHERE "DeletedAt" = {Marker};
            """);
    }
}
