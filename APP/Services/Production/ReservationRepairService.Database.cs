using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace APP.Services.Production;

public sealed partial class ReservationRepairService
{
    private async Task LockEvidence()
    {
        await db.Database.ExecuteSqlRawAsync("SET LOCAL lock_timeout = '5s'; SET LOCAL statement_timeout = '30s'");
        await db.Database.ExecuteSqlRawAsync("""
            LOCK TABLE public."Products", public."UnitOfMeasures", public."Departments",
              public."Requisitions", public."RequisitionItems", public."StockRequisitionIssues",
              public."ProductionScheduleProducts", public."ProductionSchedules", public."ProductionActivities",
              public."ProductionActivitySteps", public."Operations", public."MaterialReturnNotes", public."ProductionExtraPackings",
              public."FinalPackings", public."MaterialBatchReservedQuantities", public."MaterialBatches", public."Materials",
              public."ShelfMaterialBatches", public."WarehouseLocationShelves", public."WarehouseLocationRacks",
              public."WarehouseLocations", public."Warehouses", public."StockMigrations", public."StockMigrationLines",
              public."StockMigrationWriteOffs", public."BinCardInformation", public."MassMaterialBatchMovements",
              public."MaterialBatchEvents", public.users, public.roles, public.userroles, public.roleclaims,
              public."PermissionTypes", public."ReservationRepairs"
            IN SHARE ROW EXCLUSIVE MODE
            """);
    }

    private async Task<string> ReadPlan(Guid batchId, Guid migrationId)
    {
        await using var settings = db.Database.GetDbConnection().CreateCommand();
        settings.Transaction = db.Database.CurrentTransaction.GetDbTransaction();
        settings.CommandText = """
            SELECT set_config('oryx_reservation_repair.migration', @migration, true),
              set_config('oryx_reservation_repair.actor', @actor, true),
              set_config('oryx_reservation_repair.batch', @batch, true)
            """;
        settings.Parameters.Add(new NpgsqlParameter("migration", migrationId.ToString()));
        settings.Parameters.Add(new NpgsqlParameter("actor", identity.UserId.GetValueOrDefault().ToString()));
        settings.Parameters.Add(new NpgsqlParameter("batch", batchId.ToString()));
        await settings.ExecuteNonQueryAsync();
        using var resource = typeof(ReservationRepairService).Assembly.GetManifestResourceStream("APP.Services.Production.ReservationRepairPlan.sql")
            ?? throw new InvalidOperationException("Reservation repair plan is missing.");
        using var reader = new StreamReader(resource);
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.Transaction = db.Database.CurrentTransaction.GetDbTransaction();
        command.CommandText = await reader.ReadToEndAsync();
        command.CommandTimeout = 30;
        return (string)(await command.ExecuteScalarAsync() ?? throw new InvalidOperationException("Reservation repair plan was empty."));
    }

    private async Task<string> Rebind(int expected)
    {
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.Transaction = db.Database.CurrentTransaction.GetDbTransaction();
        command.CommandText = """
            UPDATE public."MaterialBatchReservedQuantities" q SET "MaterialBatchId" = b.new_batch_id,
              "WarehouseLocationShelfId" = b.new_shelf_id, "UpdatedAt" = transaction_timestamp(),
              "LastUpdatedById" = current_setting('oryx_reservation_repair.actor')::uuid
            FROM repair_bindings b WHERE q."Id" = b.reservation_id AND to_jsonb(q) = b.before_row
            """;
        if (await command.ExecuteNonQueryAsync() != expected)
            throw new DBConcurrencyException("A reservation changed while repairing stock.");
        command.CommandText = """
            DO $check$ BEGIN
              IF EXISTS (SELECT 1 FROM repair_items i WHERE i.approved_quantity <> (
                SELECT sum(q."Quantity") FROM public."MaterialBatchReservedQuantities" q
                JOIN public."MaterialBatches" b ON b."Id" = q."MaterialBatchId"
                WHERE q."ProductionScheduleProductId" = i.production_id AND b."MaterialId" = i.material_id
                  AND q."DeletedAt" IS NULL)) THEN
                RAISE EXCEPTION 'Repaired quantities do not reconcile' USING ERRCODE='23514';
              END IF;
            END $check$;
            SELECT jsonb_agg(to_jsonb(q) ORDER BY q."Id")::text
            FROM public."MaterialBatchReservedQuantities" q JOIN repair_bindings b ON b.reservation_id = q."Id"
            """;
        return (string)(await command.ExecuteScalarAsync() ?? throw new InvalidOperationException("Repaired reservation evidence was empty."));
    }
}
