using System.Text.Json;
using APP.IRepository;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.QualityRoutines;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class CommercialCertificateRepository(ApplicationDbContext context)
    : ICommercialCertificateRepository
{
    private sealed record ArdReport(
        Guid Id, Guid FormId, AnalysisType AnalysisType,
        List<CommercialCoaItem> Items);

    public async Task<Result<Guid>> GenerateForMaterialBatch(
        Guid materialBatchId, Guid actorId)
    {
        var sampleId = await context.MaterialSamplings
            .Where(item => item.MaterialBatchId == materialBatchId)
            .OrderByDescending(item => item.SampleDate)
            .ThenByDescending(item => item.CreatedAt)
            .Select(item => item.Id).FirstOrDefaultAsync();
        return sampleId == Guid.Empty
            ? Error.NotFound("COA.MaterialSample",
                "The material batch has no QC sample.")
            : await GenerateForMaterial(sampleId, actorId);
    }

    public async Task<Result<Guid>> GenerateForMaterial(Guid sampleId, Guid actorId)
    {
        if (await context.CommercialCertificates.AnyAsync(item =>
            item.MaterialSamplingId == sampleId))
            return Error.Conflict("COA.Exists",
                "An immutable certificate already exists for this material sample.");
        var sample = await context.MaterialSamplings
            .FirstOrDefaultAsync(item => item.Id == sampleId);
        if (sample is null)
            return Error.NotFound("COA.MaterialSample", "Material sample was not found.");
        var readiness = await QualityAnalysisReadiness.MaterialSampleAsync(
            context, sample);
        if (readiness.IsFailure) return readiness.Errors;
        if (!readiness.Value)
            return Error.Conflict("COA.Pending",
                "Chemical and configured Microbial worksheets require final approval.");
        var ards = await context.MaterialAnalyticalRawData
            .Where(item => item.Id == sample.ChemicalArdId ||
                sample.MicrobialArdId.HasValue && item.Id == sample.MicrobialArdId)
            .Include(item => item.CoaItems).ToListAsync();
        var reports = ards.Select(item => new ArdReport(
            item.Id, item.FormId, item.AnalysisType, item.CoaItems)).ToList();
        return await Create(sampleId, null, reports, actorId);
    }

    public async Task<Result<Guid>> GenerateForProduct(Guid atrId, Guid actorId)
    {
        if (await context.CommercialCertificates.AnyAsync(item =>
            item.AnalyticalTestRequestId == atrId))
            return Error.Conflict("COA.Exists",
                "An immutable certificate already exists for this product stage.");
        var atr = await context.AnalyticalTestRequests
            .FirstOrDefaultAsync(item => item.Id == atrId);
        if (atr is null)
            return Error.NotFound("COA.Atr", "Analytical test request was not found.");
        var readiness = await QualityAnalysisReadiness.ProductStageAsync(context, atr);
        if (readiness.IsFailure) return readiness.Errors;
        if (!readiness.Value)
            return Error.Conflict("COA.Pending",
                "Chemical and configured Microbial worksheets require final approval.");
        var ards = await context.ProductAnalyticalRawData
            .Where(item => item.Id == atr.ChemicalArdId ||
                atr.MicrobialArdId.HasValue && item.Id == atr.MicrobialArdId)
            .Include(item => item.CoaItems).ToListAsync();
        var reports = ards.Select(item => new ArdReport(
            item.Id, item.FormId, item.AnalysisType, item.CoaItems)).ToList();
        return await Create(null, atrId, reports, actorId,
            atr.BatchManufacturingRecordId, atr.ProductionActivityStepId);
    }

    public async Task<Result<CommercialCertificateDto>> Get(Guid id)
    {
        var item = await context.CommercialCertificates
            .FirstOrDefaultAsync(item => item.Id == id);
        if (item is null)
            return Error.NotFound("COA.Certificate", "Certificate was not found.");
        return new CommercialCertificateDto
        {
            Id = item.Id,
            Target = item.Target,
            MaterialSamplingId = item.MaterialSamplingId,
            AnalyticalTestRequestId = item.AnalyticalTestRequestId,
            CertificateCode = item.CertificateCode,
            Combined = item.Combined,
            RowsJson = item.RowsJson,
            IssuedAt = item.IssuedAt
        };
    }

    private async Task<Result<Guid>> Create(
        Guid? sampleId,
        Guid? atrId,
        IReadOnlyCollection<ArdReport> ards,
        Guid actorId,
        Guid? batchManufacturingRecordId = null,
        Guid? productionActivityStepId = null)
    {
        if (ards.Count == 0 || ards.Any(item => item.Items.Count == 0))
            return Error.Conflict("COA.Reportability",
                "Every required ARD needs a controlled list of reportable COA items.");
        var formIds = ards.Select(item => item.FormId).ToList();
        var responseQuery = context.Responses.Where(item =>
            formIds.Contains(item.FormId) && item.Approved && !item.Rejected);
        responseQuery = sampleId.HasValue
            ? responseQuery.Where(item => item.MaterialSamplingId == sampleId)
            : responseQuery.Where(item =>
                item.BatchManufacturingRecordId == batchManufacturingRecordId &&
                item.ProductionActivityStepId == productionActivityStepId);
        var responses = await responseQuery.Include(item => item.FormResponses)
            .ToListAsync();
        if (responses.Count != ards.Count)
            return Error.Conflict("COA.Responses",
                "The certificate scope does not have one approved response per ARD.");
        var rows = new List<object>();
        foreach (var ard in ards.OrderBy(item => item.AnalysisType))
        {
            var response = responses.SingleOrDefault(item => item.FormId == ard.FormId);
            if (response is null)
                return Error.Conflict("COA.Response", "An approved ARD response is missing.");
            foreach (var item in ard.Items.OrderBy(item => item.DisplayOrder))
            {
                var result = response.FormResponses.SingleOrDefault(value =>
                    value.FormFieldId == item.FormFieldId);
                if (result is null || string.IsNullOrWhiteSpace(result.Value) ||
                    result.Value.Trim() == "-" || result.Complies == false)
                    return Error.Conflict("COA.Result",
                        $"Reportable result is missing or noncompliant: {item.DisplayLabel}.");
                rows.Add(new
                {
                    ard.AnalysisType,
                    ardId = ard.Id,
                    responseId = response.Id,
                    item.FormFieldId,
                    item.DisplayLabel,
                    item.GroupName,
                    item.SpecificationText,
                    item.Unit,
                    item.Reference,
                    result = result.Value,
                    result.Complies
                });
            }
        }
        var now = DateTime.UtcNow;
        var certificate = new CommercialCertificate
        {
            Id = Guid.NewGuid(),
            Target = sampleId.HasValue
                ? CommercialCertificateTarget.Material
                : CommercialCertificateTarget.Product,
            MaterialSamplingId = sampleId,
            AnalyticalTestRequestId = atrId,
            CertificateCode = $"COA/{now:yyyy}/{Guid.NewGuid():N}",
            Combined = ards.Any(item => item.AnalysisType == AnalysisType.Chemical) &&
                ards.Any(item => item.AnalysisType == AnalysisType.Microbial),
            RowsJson = JsonSerializer.Serialize(rows),
            IssuedAt = now,
            IssuedById = actorId,
            CreatedById = actorId
        };
        context.CommercialCertificates.Add(certificate);
        await context.SaveChangesAsync();
        return certificate.Id;
    }
}
