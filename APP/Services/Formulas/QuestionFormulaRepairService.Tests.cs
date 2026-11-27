using APP.Utils;
using DOMAIN.Entities.AnalyticalTestRequests;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using Microsoft.EntityFrameworkCore;

#nullable enable
namespace APP.Services.Formulas;

public sealed partial class QuestionFormulaRepairService
{
    /// <summary>
    /// A test whose formula fields can be repaired: a product analytical test request, or a raw or packaging material sampling
    /// (the material test from a goods receipt note or a retest). Its id is what the repair API calls <c>AtrId</c>.
    /// </summary>
    private sealed record RepairTest(Guid Id, string? ArNumber, MaterialKind? MaterialKind,
        Guid? BatchManufacturingRecordId, Guid? ProductionActivityStepId, Guid? MaterialBatchId,
        Guid? ChemicalArdId, Guid? MicrobialArdId, string Status, bool Open)
    {
        public bool IsMaterial => MaterialKind.HasValue;

        public static RepairTest Product(AnalyticalTestRequest atr) => new(atr.Id, atr.ArNumber, null,
            atr.BatchManufacturingRecordId, atr.ProductionActivityStepId, null, atr.ChemicalArdId, atr.MicrobialArdId, atr.Status.ToString(),
            !atr.ReleasedAt.HasValue && atr.Status is AnalyticalTestStatus.Testing or AnalyticalTestStatus.Assigned);

        // A material is under test while its batch is assigned for testing or being tested, as an ATR is.
        public static RepairTest Material(Guid id, string? arNumber, Guid batchId, Guid? chemicalArdId, Guid? microbialArdId, MaterialKind kind, BatchStatus status) =>
            new(id, arNumber, kind, null, null, batchId, chemicalArdId, microbialArdId, status.ToString(),
                status is BatchStatus.Testing or BatchStatus.TestAssigned);

        /// <summary>Whether a worksheet response belongs to this test. Material responses saved before samples were bound have no sample.</summary>
        public bool Owns(Response response) => IsMaterial
            ? response.MaterialBatchId == MaterialBatchId && (response.MaterialSamplingId == Id || response.MaterialSamplingId == null)
                && response.BatchManufacturingRecordId == null && response.RoutineTrackId == null
            : response.BatchManufacturingRecordId == BatchManufacturingRecordId && response.ProductionActivityStepId == ProductionActivityStepId;
    }

    /// <summary>The product test or material sampling with this id; a browser-supplied kind is never trusted.</summary>
    private async Task<RepairTest?> LoadTest(Guid id)
    {
        var atr = await db.AnalyticalTestRequests.AsNoTracking().SingleOrDefaultAsync(a => a.Id == id);
        if (atr is not null) return RepairTest.Product(atr);
        var sampling = await db.MaterialSamplings.AsNoTracking().Where(s => s.Id == id)
            .Select(s => new { s.Id, s.ArNumber, s.MaterialBatchId, s.ChemicalArdId, s.MicrobialArdId, s.MaterialBatch.Status, Kind = (MaterialKind?)s.MaterialBatch.Material.Kind })
            .SingleOrDefaultAsync();
        return sampling?.Kind is { } kind
            ? RepairTest.Material(sampling.Id, sampling.ArNumber, sampling.MaterialBatchId, sampling.ChemicalArdId, sampling.MicrobialArdId, kind, sampling.Status)
            : null;
    }

    /// <summary>The product and material tests on which the caller is assigned to one of <paramref name="fieldIds"/>.</summary>
    private async Task<List<RepairTest>> AssignedTests(IReadOnlyCollection<Guid> fieldIds)
    {
        var assignments = db.FormFieldAssignees.Where(a => a.AssigneeId == identity.UserId && fieldIds.Contains(a.FormFieldId));
        var atrs = await db.AnalyticalTestRequests.AsNoTracking().Where(atr => assignments.Any(a =>
            a.FormAssignee.BatchManufacturingRecordId == atr.BatchManufacturingRecordId &&
            a.FormAssignee.ProductionActivityStepId == atr.ProductionActivityStepId)).ToListAsync();
        var samplings = await db.MaterialSamplings.AsNoTracking().Where(s => assignments.Any(a =>
                a.FormAssignee.MaterialBatchId == s.MaterialBatchId && (a.FormAssignee.MaterialSamplingId == s.Id || a.FormAssignee.MaterialSamplingId == null)))
            .Select(s => new { s.Id, s.ArNumber, s.MaterialBatchId, s.ChemicalArdId, s.MicrobialArdId, s.MaterialBatch.Status, Kind = (MaterialKind?)s.MaterialBatch.Material.Kind })
            .ToListAsync();
        return atrs.Select(RepairTest.Product)
            .Concat(samplings.Where(s => s.Kind.HasValue).Select(s => RepairTest.Material(s.Id, s.ArNumber, s.MaterialBatchId, s.ChemicalArdId, s.MicrobialArdId, s.Kind!.Value, s.Status)))
            .ToList();
    }

    /// <summary>The worksheet forms of the test's chemical and microbial raw data.</summary>
    private Task<List<Guid>> TestForms(RepairTest test) => test.IsMaterial
        ? db.MaterialAnalyticalRawData.Where(ard => ard.Id == test.ChemicalArdId || ard.Id == test.MicrobialArdId).Select(ard => ard.FormId).ToListAsync()
        : db.ProductAnalyticalRawData.Where(ard => ard.Id == test.ChemicalArdId || ard.Id == test.MicrobialArdId).Select(ard => ard.FormId).ToListAsync();

    /// <summary>The permissions to enter readings on this kind of test: start the test and view its tests.</summary>
    private static string? TestPermissionProblem(RepairTest test, HashSet<string> grants) => test.MaterialKind switch
    {
        null => grants.Contains(PermissionKeys.CanStartProductTest) &&
            (grants.Contains(PermissionKeys.CanViewProductAnalyticalTestRequests) || grants.Contains(PermissionKeys.CanViewProductAnalyticalTestRequestsForAnalysing))
            ? null : "You need CanStartProductTest and a product test view permission to repair this test.",
        MaterialKind.Raw => grants.Contains(PermissionKeys.CanStartRawMaterialTest) && grants.Contains(PermissionKeys.CanViewRawMaterialGoodsReceiptNotes)
            ? null : "You need CanStartRawMaterialTest and CanViewRawMaterialGoodsReceiptNotes to repair this raw material test.",
        _ => grants.Contains(PermissionKeys.CanStartPackagingMaterialTest) && grants.Contains(PermissionKeys.CanViewPackagingMaterialGoodsReceiptNotes)
            ? null : "You need CanStartPackagingMaterialTest and CanViewPackagingMaterialGoodsReceiptNotes to repair this packaging material test.",
    };

    /// <summary>Whether the caller is assigned to the field on this test, as the worksheet requires to enter its readings.</summary>
    private Task<bool> AssignedTo(RepairTest test, FormField field) => test.IsMaterial
        ? db.FormFieldAssignees.AnyAsync(a => a.FormFieldId == field.Id && a.AssigneeId == identity.UserId &&
            a.FormAssignee.FormId == field.FormSection.FormId && a.FormAssignee.MaterialBatchId == test.MaterialBatchId &&
            (a.FormAssignee.MaterialSamplingId == test.Id || a.FormAssignee.MaterialSamplingId == null) &&
            a.FormAssignee.BatchManufacturingRecordId == null && a.FormAssignee.RoutineTrackId == null)
        : db.FormFieldAssignees.AnyAsync(a => a.FormFieldId == field.Id && a.AssigneeId == identity.UserId &&
            a.FormAssignee.FormId == field.FormSection.FormId && a.FormAssignee.BatchManufacturingRecordId == test.BatchManufacturingRecordId &&
            a.FormAssignee.ProductionActivityStepId == test.ProductionActivityStepId && a.FormAssignee.MaterialBatchId == null && a.FormAssignee.RoutineTrackId == null);

    /// <summary>A response for the test's batch or step that this test does not own (another sample, another form).</summary>
    private Task<bool> ForeignResponse(RepairTest test) => test.IsMaterial
        ? db.Responses.IgnoreQueryFilters().AnyAsync(r => r.MaterialBatchId == test.MaterialBatchId && r.MaterialSamplingId != null && r.MaterialSamplingId != test.Id)
        : db.Responses.IgnoreQueryFilters().AnyAsync(r => r.BatchManufacturingRecordId == test.BatchManufacturingRecordId && r.ProductionActivityStepId == test.ProductionActivityStepId);
}
