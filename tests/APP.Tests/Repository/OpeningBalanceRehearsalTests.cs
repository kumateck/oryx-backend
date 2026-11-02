using System.Text.Json;
using APP.Repository;
using DOMAIN.Entities.Materials.StockMigrations;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

public sealed class LocalOpeningBalanceRehearsalAttribute : FactAttribute
{
    public LocalOpeningBalanceRehearsalAttribute()
    {
        if (Environment.GetEnvironmentVariable("ORYX_OPENING_BALANCE_REHEARSAL") != "1")
            Skip = "Explicitly opt in to the isolated local opening-balance rehearsal database.";
    }
}

public class OpeningBalanceRehearsalTests
{
    private const string Database = "oryx_rehearsal_20261101_203716";
    private sealed class Actor(Guid id) : ICurrentUserService
    {
        public Guid? UserId => id;
        public Guid? DepartmentId => null;
        public string DepartmentType => string.Empty;
    }

    private static ApplicationDbContext Context(Guid actor) => new(
        new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(
            $"Host=localhost;Port=5432;Database={Database};Username={Environment.UserName}").Options, new Actor(actor));

    [LocalOpeningBalanceRehearsal]
    public async Task Four_real_files_remain_drafts_until_review_then_approve_once_with_reconciled_stock()
    {
        var folder = Environment.GetEnvironmentVariable("ORYX_OPENING_BALANCE_FILES") ?? throw new InvalidOperationException("Missing rehearsal files folder");
        var reportFile = Environment.GetEnvironmentVariable("ORYX_OPENING_BALANCE_REPORT") ?? throw new InvalidOperationException("Missing report path");
        await using var initial = Context(Guid.Empty);
        Assert.Equal(Database, initial.Database.GetDbConnection().Database);
        Assert.Equal(0, await initial.MaterialBatches.CountAsync());
        Assert.Equal(0, await initial.StockMigrations.CountAsync());
        var actor = await initial.Users.OrderBy(u => u.CreatedAt).Select(u => u.Id).FirstAsync();
        var materialCount = await initial.Materials.IgnoreQueryFilters().CountAsync();
        var linkCount = await initial.MaterialDepartments.IgnoreQueryFilters().CountAsync();
        var syrup = Guid.Parse("01994fb8-2a6d-7e43-8c9b-58971da9b247");
        var files = new[]
        {
            (Name:"syrup-packaging-material-stock.xlsx", Department:syrup, Count:274),
            (Name:"syrup-raw-material-stock.xlsx", Department:syrup, Count:176),
            (Name:"tablet-raw-material-stock.xlsx", Department:Guid.Parse("01994fb8-2b57-7b23-a5b9-5b5c68aa9abc"), Count:305),
            (Name:"ointment-raw-material-stock.xlsx", Department:Guid.Parse("01994fb8-2b51-7c50-9f4b-31c7afec0ee2"), Count:64),
        };
        var drafts = new List<(Guid Id, string File, int Count)>();
        foreach (var file in files)
        {
            await using var context = Context(actor);
            await using var stream = File.OpenRead(Path.Combine(folder, file.Name));
            var upload = new FormFile(stream, 0, stream.Length, "file", file.Name);
            var result = await new StockMigrationRepository(context).Upload(upload, file.Department, "mg", "ml", actor);
            Assert.True(result.IsSuccess, result.Error?.Description);
            drafts.Add((result.Value, file.Name, file.Count));
        }
        await using (var context = Context(actor))
        {
            Assert.Equal(819, await context.StockMigrationLines.CountAsync());
            Assert.Equal(4, await context.StockMigrations.CountAsync(m => m.Status == StockMigrationStatus.Draft));
            Assert.Equal(0, await context.MaterialBatches.CountAsync());
            Assert.Equal(0, await context.ShelfMaterialBatches.CountAsync());
            Assert.Equal(0, await context.BinCardInformation.CountAsync());
        }
        var results = new List<RehearsalResult>();
        foreach (var draft in drafts)
        {
            await using var context = Context(actor);
            var repository = new StockMigrationRepository(context);
            var lines = await context.StockMigrationLines.Where(l => l.StockMigrationId == draft.Id).ToListAsync();
            Assert.Equal(draft.Count, lines.Count);
            var blocked = lines.Where(l => l.Status == StockMigrationLineStatus.Blocked).ToList();
            var blockers = blocked.Select(l => new RehearsalBlocker(l.RowNumber, l.MaterialCode, l.SheetMaterialName, l.Blockers)).ToList();
            if (blocked.Count > 0)
            {
                var count = await context.MaterialBatches.CountAsync();
                var refused = await repository.Approve(draft.Id, actor);
                Assert.True(refused.IsFailure);
                Assert.Equal(count, await context.MaterialBatches.CountAsync());
                // This exclusion exists ONLY on the rehearsal copy to exercise approval. The
                // deliverable files retain these rows for the user's actual corrections.
                var excluded = await repository.BulkUpdate(draft.Id, new BulkUpdateStockMigrationLinesRequest
                    { LineIds = blocked.Select(l => l.Id).ToList(), Excluded = true });
                Assert.True(excluded.IsSuccess);
            }
            var line = lines.First(l => !l.Excluded && l.Status is StockMigrationLineStatus.Ok or StockMigrationLineStatus.Warning);
            var corrected = await repository.UpdateLine(draft.Id, line.Id, new UpdateStockMigrationLineRequest
            {
                MaterialCode = line.MaterialCode, MaterialId = line.ResolvedMaterialId,
                WarehouseName = line.WarehouseName, ShelfName = line.ShelfName, ShelfId = line.ResolvedShelfId,
                BatchNumber = line.BatchNumber, UnitSymbol = line.UnitSymbol, QuantityInSheet = line.QuantityInSheet,
                ManufacturingDate = line.ManufacturingDate, ExpiryDate = line.ExpiryDate,
            });
            Assert.True(corrected.IsSuccess);
            var expected = await context.StockMigrationLines.Where(l => l.StockMigrationId == draft.Id && !l.Excluded).SumAsync(l => l.Quantity ?? 0);
            var approved = await repository.Approve(draft.Id, actor);
            Assert.True(approved.IsSuccess, approved.Error?.Description);
            var importedIds = await context.StockMigrationLines.Where(l => l.StockMigrationId == draft.Id && l.ImportedBatchId != null).Select(l => l.ImportedBatchId.Value).Distinct().ToListAsync();
            var batches = await context.MaterialBatches.Where(b => importedIds.Contains(b.Id)).ToListAsync();
            Assert.Equal(expected, batches.Sum(b => b.TotalQuantity));
            foreach (var batch in batches)
            {
                Assert.Equal(batch.TotalQuantity, batch.QuantityAssigned);
                Assert.Equal(batch.TotalQuantity, await context.ShelfMaterialBatches.Where(s => s.MaterialBatchId == batch.Id).SumAsync(s => s.Quantity));
                Assert.Equal(batch.TotalQuantity, await context.BinCardInformation.Where(s => s.MaterialBatchId == batch.Id).SumAsync(s => s.QuantityReceived));
            }
            var batchCount = await context.MaterialBatches.CountAsync();
            Assert.True((await repository.Approve(draft.Id, actor)).IsFailure);
            Assert.Equal(batchCount, await context.MaterialBatches.CountAsync());
            Assert.True((await repository.BulkUpdate(draft.Id, new BulkUpdateStockMigrationLinesRequest { LineIds = [line.Id], Excluded = true })).IsFailure);
            results.Add(new(draft.File, draft.Count, draft.Count - blocked.Count, blockers));
        }
        await using var final = Context(actor);
        Assert.Equal(materialCount, await final.Materials.IgnoreQueryFilters().CountAsync());
        Assert.Equal(linkCount, await final.MaterialDepartments.IgnoreQueryFilters().CountAsync());
        var folidyn = await final.MaterialBatches.Where(b => b.Material.Name == "FOLIDYN TABLET").ToListAsync();
        Assert.Equal(13, folidyn.Count);
        Assert.Equal(13, folidyn.Select(b => b.BatchNumber).Distinct().Count());
        var expired = await final.MaterialBatches.CountAsync(b => b.ExpiryDate < DateTime.UtcNow.Date);
        Assert.True(expired > 0);
        await File.WriteAllTextAsync(reportFile, JsonSerializer.Serialize(new
        {
            Database, DraftRows = 819, DraftsHadZeroStock = true, RepeatedApprovalRefused = true,
            ApprovedDraftCorrectionsRefused = true, MasterDataUnchanged = true, FolidynDistinctBatches = 13,
            ExpiredBatchesRetained = expired, RehearsalOnlyExclusions = true, Results = results,
        }, new JsonSerializerOptions { WriteIndented = true }));
    }

    private record RehearsalResult(string File, int DraftRows, int ApprovedRows, List<RehearsalBlocker> Blockers);
    private record RehearsalBlocker(int WorkbookRow, string MaterialCode, string MaterialName, string Blockers);
}
