using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class ChangeShiftTypeTimesToTimeOnly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ShiftType.StartTime/EndTime were stored as "hh:mm tt" text (e.g. "06:00 AM").
            // A plain AlterColumn to `time` fails against existing data because Postgres
            // cannot implicitly cast that text to `time` - it needs an explicit USING
            // conversion. Any null value is backfilled to midnight before the type change,
            // since the new column is NOT NULL.
            migrationBuilder.Sql(
                """
                UPDATE "ShiftTypes" SET "StartTime" = '12:00 AM' WHERE "StartTime" IS NULL;
                UPDATE "ShiftTypes" SET "EndTime" = '12:00 AM' WHERE "EndTime" IS NULL;

                ALTER TABLE "ShiftTypes"
                    ALTER COLUMN "StartTime" TYPE time without time zone
                    USING to_timestamp("StartTime", 'HH12:MI AM')::time,
                    ALTER COLUMN "StartTime" SET NOT NULL,
                    ALTER COLUMN "StartTime" SET DEFAULT '00:00:00';

                ALTER TABLE "ShiftTypes"
                    ALTER COLUMN "EndTime" TYPE time without time zone
                    USING to_timestamp("EndTime", 'HH12:MI AM')::time,
                    ALTER COLUMN "EndTime" SET NOT NULL,
                    ALTER COLUMN "EndTime" SET DEFAULT '00:00:00';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "ShiftTypes"
                    ALTER COLUMN "StartTime" TYPE text
                    USING to_char("StartTime", 'HH12:MI AM'),
                    ALTER COLUMN "StartTime" DROP NOT NULL,
                    ALTER COLUMN "StartTime" DROP DEFAULT;

                ALTER TABLE "ShiftTypes"
                    ALTER COLUMN "EndTime" TYPE text
                    USING to_char("EndTime", 'HH12:MI AM'),
                    ALTER COLUMN "EndTime" DROP NOT NULL,
                    ALTER COLUMN "EndTime" DROP DEFAULT;
                """);
        }
    }
}
