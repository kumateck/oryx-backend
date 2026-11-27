using System.Data;
using System.Globalization;
using APP.Services.Authorization;
using APP.Services.Forms;
using APP.Utils;
using DOMAIN.Entities.ProductSpecifications;
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
        if (context.Database.IsRelational() && context.Database.CurrentTransaction is null)
        {
            await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var generated = await GenerateForProduct(atrId, actorId);
            if (generated.IsSuccess) await transaction.CommitAsync();
            return generated;
        }
        if (!(await CurrentPermissionSet.Load(context, actorId)).Contains(PermissionKeys.CanCheckProductTest))
            return Error.Forbidden("COA.Permission", "You cannot generate a product certificate.");
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
        var fieldReferences = await context.FormFields.AsNoTracking().Where(field => formIds.Contains(field.FormSection.FormId))
            .Select(field => new { field.Id, field.FormSectionId, QuestionReference = field.Question.Reference })
            .ToDictionaryAsync(field => field.Id);
        var rows = new List<object>();
        foreach (var ard in ards.OrderBy(item => item.AnalysisType))
        {
            var response = responses.SingleOrDefault(item => item.FormId == ard.FormId);
            if (response is null)
                return Error.Conflict("COA.Response", "An approved ARD response is missing.");
            var sectionReferences = await SpecificationTestReferences.Values(context, response);
            // A typed microbial limit on the specification prints instead of the COA item's typed-in text.
            var microbialLimitTexts = await APP.Services.Forms.Microbial.MicrobialLimitRuntime.Texts(context, response);
            // The ARD's COA items set the printed label and order; the template and specification decide whether a test prints at all.
            var hiddenFields = await APP.Services.Forms.CoaReporting.CoaReportingRuntime.HiddenFields(context, response);
            var reportedItems = ard.Items.Where(item => !hiddenFields.Contains(item.FormFieldId)).ToList();
            if (reportedItems.Count == 0)
                return Error.Conflict("COA.Reportability",
                    "No test on this ARD is reported on the certificate. Tick \"Report on COA\" on the template or the specification.");
            foreach (var item in reportedItems.OrderBy(item => item.DisplayOrder))
            {
                var result = response.FormResponses.SingleOrDefault(value =>
                    value.FormFieldId == item.FormFieldId);
                if (result is null || string.IsNullOrWhiteSpace(result.Value) ||
                    result.Value.Trim() == "-" || result.Complies == false)
                    return Error.Conflict("COA.Result",
                        $"Reportable result is missing or noncompliant: {item.DisplayLabel}.");
                var reported = response.AssayReportingSnapshotJson is null ? null
                    : JsonSerializer.Deserialize<AssayReportingSnapshot>(response.AssayReportingSnapshotJson)?.Fields
                        .SingleOrDefault(field => field.FormFieldId == item.FormFieldId);
                if (reported is not null && (!string.IsNullOrWhiteSpace(reported.Error) || reported.Results.Any(value => value.Converted && value.Complies != true)))
                    return Error.Conflict("COA.AssayReporting", "The captured assay reporting result is unresolved or noncompliant.");
                var reportingLimits = reported?.Results.Where(value => value.Converted).Select(value =>
                    $"{System.Net.WebUtility.HtmlEncode(value.Label)}: {value.Minimum.ToString(CultureInfo.InvariantCulture)}–{value.Maximum.ToString(CultureInfo.InvariantCulture)} {value.Unit}").ToList();
                var specification = reportingLimits is { Count: > 0 }
                    ? (reported.Results.Any(value => !value.Converted) ? item.SpecificationText + "<br/>" : "") + string.Join("<br/>", reportingLimits)
                    : microbialLimitTexts.GetValueOrDefault(item.FormFieldId) ?? item.SpecificationText;
                var fieldReference = fieldReferences.GetValueOrDefault(item.FormFieldId);
                var reference = StoredReference(item.Reference) ?? StoredReference(fieldReference is null ? null : sectionReferences.GetValueOrDefault(fieldReference.FormSectionId)) ?? StoredReference(fieldReference?.QuestionReference);
                rows.Add(new
                {
                    ard.AnalysisType,
                    ardId = ard.Id,
                    responseId = response.Id,
                    item.FormFieldId,
                    item.DisplayLabel,
                    item.GroupName,
                    SpecificationText = specification,
                    response.CertificateNotes,
                    Unit = reported is not null && reported.Results.All(value => value.Converted) ? "% w/v" : item.Unit,
                    Reference = reference,
                    result = reported is null ? result.Value : JsonSerializer.Serialize(new { originalResponse = result.Value, coaReportedResults = reported.Results }, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
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
        if (atrId.HasValue && !(await CurrentPermissionSet.Load(context, actorId)).Contains(PermissionKeys.CanCheckProductTest))
            return Error.Forbidden("COA.Permission", "Your product certificate permission changed. Reload before generating it.");
        context.CommercialCertificates.Add(certificate);
        await context.SaveChangesAsync();
        return certificate.Id;
    }
    private static string StoredReference(string value) => value?.Trim() switch
    {
        null or "" or "-" or "–" or "—" => null,
        "0" => "BP", "1" => "USP", "2" => "Ph. Int.", "3" => "In-house",
        var reference => reference
    };

}
