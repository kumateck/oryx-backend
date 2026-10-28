using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class RestoreItemTransactionTypeText : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Existing installations have text from the initial migration. Databases created
            // from the later enum model may have integer storage instead. Never cast names
            // to numbers, rewrite balances, or guess the meaning of an unknown ordinal.
            migrationBuilder.Sql("""
                DO $ledger$
                DECLARE storage_type text;
                BEGIN
                    SELECT data_type INTO storage_type
                    FROM information_schema.columns
                    WHERE table_schema = current_schema()
                      AND table_name = 'ItemTransactionLogs' AND column_name = 'TransactionType';
                    IF storage_type = 'integer' THEN
                        LOCK TABLE "ItemTransactionLogs" IN ACCESS EXCLUSIVE MODE;
                        IF EXISTS (SELECT 1 FROM "ItemTransactionLogs" WHERE "TransactionType" IS NULL OR "TransactionType" NOT BETWEEN 0 AND 5) THEN
                            RAISE EXCEPTION 'Unknown inventory transaction type; review the ledger before converting its storage';
                        END IF;
                        ALTER TABLE "ItemTransactionLogs" ALTER COLUMN "TransactionType" TYPE text
                        USING CASE "TransactionType"
                            WHEN 0 THEN 'Damaged' WHEN 1 THEN 'Missing' WHEN 2 THEN 'Returned'
                            WHEN 3 THEN 'Issued' WHEN 4 THEN 'Purchased' WHEN 5 THEN 'Adjustment'
                        END;
                    ELSIF storage_type IS DISTINCT FROM 'text' THEN
                        RAISE EXCEPTION 'Unexpected inventory transaction type storage: %', storage_type;
                    END IF;
                END $ledger$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Keep the original text contract and ledger evidence on application rollback.
        }
    }
}
