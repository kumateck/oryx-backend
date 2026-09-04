using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeHrPayrollRunTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF to_regclass('public."HrPayrollRuns"') IS NULL
                       AND to_regclass('public."PayrollRuns"') IS NOT NULL
                       AND EXISTS (
                           SELECT 1
                           FROM information_schema.columns
                           WHERE table_schema = 'public'
                             AND table_name = 'PayrollRuns'
                             AND column_name = 'PeriodStart'
                       )
                       AND NOT EXISTS (
                           SELECT 1
                           FROM information_schema.columns
                           WHERE table_schema = 'public'
                             AND table_name = 'PayrollRuns'
                             AND column_name = 'PayrollCompanyId'
                       ) THEN
                        ALTER TABLE "PayrollRuns" RENAME TO "HrPayrollRuns";
                        ALTER TABLE "HrPayrollRuns"
                            RENAME CONSTRAINT "PK_PayrollRuns" TO "PK_HrPayrollRuns";
                        ALTER TABLE "HrPayrollRuns"
                            RENAME CONSTRAINT "FK_PayrollRuns_users_CreatedById"
                            TO "FK_HrPayrollRuns_users_CreatedById";
                        ALTER TABLE "HrPayrollRuns"
                            RENAME CONSTRAINT "FK_PayrollRuns_users_LastDeletedById"
                            TO "FK_HrPayrollRuns_users_LastDeletedById";
                        ALTER TABLE "HrPayrollRuns"
                            RENAME CONSTRAINT "FK_PayrollRuns_users_LastUpdatedById"
                            TO "FK_HrPayrollRuns_users_LastUpdatedById";
                        ALTER TABLE "PayrollRunApprovals"
                            RENAME CONSTRAINT "FK_PayrollRunApprovals_PayrollRuns_PayrollRunId"
                            TO "FK_PayrollRunApprovals_HrPayrollRuns_PayrollRunId";
                        ALTER TABLE "Payslips"
                            RENAME CONSTRAINT "FK_Payslips_PayrollRuns_PayrollRunId"
                            TO "FK_Payslips_HrPayrollRuns_PayrollRunId";
                        ALTER INDEX "IX_PayrollRuns_CreatedById"
                            RENAME TO "IX_HrPayrollRuns_CreatedById";
                        ALTER INDEX "IX_PayrollRuns_LastDeletedById"
                            RENAME TO "IX_HrPayrollRuns_LastDeletedById";
                        ALTER INDEX "IX_PayrollRuns_LastUpdatedById"
                            RENAME TO "IX_HrPayrollRuns_LastUpdatedById";
                        COMMENT ON TABLE "HrPayrollRuns" IS
                            'oryx:renamed-from-payrollruns-by-20260903233548';
                    END IF;
                END $$;
                """
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF to_regclass('public."HrPayrollRuns"') IS NOT NULL
                       AND obj_description(
                           'public."HrPayrollRuns"'::regclass,
                           'pg_class'
                       ) = 'oryx:renamed-from-payrollruns-by-20260903233548' THEN
                        COMMENT ON TABLE "HrPayrollRuns" IS NULL;
                        ALTER TABLE "HrPayrollRuns"
                            RENAME CONSTRAINT "PK_HrPayrollRuns" TO "PK_PayrollRuns";
                        ALTER TABLE "HrPayrollRuns"
                            RENAME CONSTRAINT "FK_HrPayrollRuns_users_CreatedById"
                            TO "FK_PayrollRuns_users_CreatedById";
                        ALTER TABLE "HrPayrollRuns"
                            RENAME CONSTRAINT "FK_HrPayrollRuns_users_LastDeletedById"
                            TO "FK_PayrollRuns_users_LastDeletedById";
                        ALTER TABLE "HrPayrollRuns"
                            RENAME CONSTRAINT "FK_HrPayrollRuns_users_LastUpdatedById"
                            TO "FK_PayrollRuns_users_LastUpdatedById";
                        ALTER TABLE "PayrollRunApprovals"
                            RENAME CONSTRAINT "FK_PayrollRunApprovals_HrPayrollRuns_PayrollRunId"
                            TO "FK_PayrollRunApprovals_PayrollRuns_PayrollRunId";
                        ALTER TABLE "Payslips"
                            RENAME CONSTRAINT "FK_Payslips_HrPayrollRuns_PayrollRunId"
                            TO "FK_Payslips_PayrollRuns_PayrollRunId";
                        ALTER INDEX "IX_HrPayrollRuns_CreatedById"
                            RENAME TO "IX_PayrollRuns_CreatedById";
                        ALTER INDEX "IX_HrPayrollRuns_LastDeletedById"
                            RENAME TO "IX_PayrollRuns_LastDeletedById";
                        ALTER INDEX "IX_HrPayrollRuns_LastUpdatedById"
                            RENAME TO "IX_PayrollRuns_LastUpdatedById";
                        ALTER TABLE "HrPayrollRuns" RENAME TO "PayrollRuns";
                    END IF;
                END $$;
                """
            );
        }
    }
}
