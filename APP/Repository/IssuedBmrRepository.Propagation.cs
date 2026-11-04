using System.Text.Json;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.QcWorksheets;
using Microsoft.EntityFrameworkCore;

namespace APP.Repository;

public sealed partial class IssuedBmrRepository
{
    private async Task Apply(BatchManufacturingRecord record, string number, DateTime? made, DateTime? expires,
        Guid actor, string reason, Guid? revisionId)
    {
        var batchId = record.ProductionScheduleProductId;
        var previousNumber = record.BatchNumber;
        var change = new IssuedBmrChange { Id = Guid.NewGuid(), BatchManufacturingRecordId = record.Id, ActorId = actor,
            OccurredAt = DateTime.UtcNow, Reason = reason, RevisionId = revisionId, PreviousBatchNumber = previousNumber,
            PreviousManufacturingDate = record.ManufacturingDate, PreviousExpiryDate = record.ExpiryDate,
            BatchNumber = number, ManufacturingDate = made, ExpiryDate = expires };
        record.BatchNumber = number; record.ManufacturingDate = made; record.ExpiryDate = expires;
        record.LastUpdatedById = actor; record.UpdatedAt = change.OccurredAt;
        record.ProductionScheduleProduct.BatchNumber = number;
        var packaging = await context.BatchPackagingRecords.IgnoreQueryFilters().IgnoreAutoIncludes()
            .Where(item => item.ProductionScheduleProductId == batchId && item.DeletedAt == null).ToListAsync();
        foreach (var item in packaging) { item.BatchNumber = number; item.ManufacturingDate = made; item.ExpiryDate = expires; }
        var analyses = await context.AnalyticalTestRequests.IgnoreQueryFilters().IgnoreAutoIncludes()
            .Where(item => item.BatchManufacturingRecordId == record.Id).ToListAsync();
        foreach (var item in analyses) { if (made.HasValue) item.ManufacturingDate = made.Value; if (expires.HasValue) item.ExpiryDate = expires.Value; }
        var returns = await context.MaterialReturnNotes.IgnoreQueryFilters().IgnoreAutoIncludes()
            .Where(item => item.ProductionScheduleProductId == batchId && item.DeletedAt == null).ToListAsync();
        foreach (var item in returns) item.BatchNumber = number;
        var ledger = await context.BinCardInformation.IgnoreQueryFilters().IgnoreAutoIncludes()
            .Where(item => item.RequisitionId.HasValue && context.Requisitions.IgnoreQueryFilters()
                .Any(requisition => requisition.Id == item.RequisitionId && requisition.ProductionScheduleProductId == batchId)).ToListAsync();
        foreach (var item in ledger) item.ProductBatchNumber = number;
        var subjects = await context.QcTestRequestSubjects.IgnoreQueryFilters().IgnoreAutoIncludes()
            .Where(item => item.BatchManufacturingRecordId == record.Id).ToListAsync();
        foreach (var item in subjects) item.SubjectRef = number;
        var investigations = await context.OosInvestigations.IgnoreQueryFilters().IgnoreAutoIncludes()
            .Where(item => item.AnalyticalTestRequestId.HasValue && item.AnalyticalTestRequest.BatchManufacturingRecordId == record.Id).ToListAsync();
        foreach (var item in investigations) item.BatchNumber = number;
        var orders = await context.WorkOrders.IgnoreQueryFilters().IgnoreAutoIncludes()
            .Where(item => item.ProductionScheduleId == record.ProductionScheduleProduct.ProductionScheduleId
                && item.BatchNumber == previousNumber && item.DeletedAt == null).ToListAsync();
        foreach (var item in orders) item.BatchNumber = number;
        // Published/superseded certificates are immutable snapshots; only unissued drafts follow live corrections.
        var drafts = await context.Coas.IgnoreQueryFilters().IgnoreAutoIncludes()
            .Where(item => item.Status == CoaStatus.Draft && item.DeletedAt == null
                && item.TestRequest.Subjects.Count == 1 && item.TestRequest.Subjects.Any(subject => subject.BatchManufacturingRecordId == record.Id)).ToListAsync();
        foreach (var item in drafts) { item.BatchNumber = number; item.ManufacturingDate = made; item.ExpiryDate = expires; }
        var draftIds = drafts.Select(item => item.Id).ToList();
        var draftRows = await context.CoaRows.IgnoreQueryFilters().IgnoreAutoIncludes().Where(item => draftIds.Contains(item.CoaId)
            && item.TestRequestSubject.BatchManufacturingRecordId == record.Id).ToListAsync();
        foreach (var item in draftRows) item.SubjectRef = number;
        change.AffectedRecordsJson = JsonSerializer.Serialize(new { Packaging = packaging.Select(item => item.Id), Analyses = analyses.Select(item => item.Id),
            Returns = returns.Select(item => item.Id), Ledger = ledger.Select(item => item.Id), Subjects = subjects.Select(item => item.Id),
            Investigations = investigations.Select(item => item.Id), WorkOrders = orders.Select(item => item.Id), DraftCertificates = draftIds });
        context.IssuedBmrChanges.Add(change);
    }
}
