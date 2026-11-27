using APP.Services.Authorization;
using APP.Utils;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.QualityRoutines;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

#nullable enable
namespace APP.Services.Forms.CoaReporting;

/// <summary>
/// Which tests of a product or material specification's templates print on the certificate of analysis.
/// The template sets a default per field ("Report on COA"); the specification may override it per field.
/// Specification rows that share a number are one document: saving replaces the overrides on every row and clears their approval.
/// </summary>
public sealed class SpecificationCoaReportingService(ApplicationDbContext db)
{
    public enum Subject { Product, Material }

    private sealed record Target(Subject Subject, Guid Id, string? Number, Guid? ChemicalFormId, Guid? MicrobialFormId, MaterialKind? Kind)
    {
        public Guid? FormId(AnalysisType type) => type == AnalysisType.Microbial ? MicrobialFormId : ChemicalFormId;
        /// <summary>The edit key for this specification's own kind (raw or packaging for materials).</summary>
        public string? EditKey => Subject == Subject.Product
            ? PermissionKeys.CanEditProductCoaReportedTests
            : QcDocumentPermissions.MaterialKey(QcDocument.MaterialSpecification, QcTask.EditCoaReportedTests, Kind ?? MaterialKind.Raw);
    }

    private sealed record Field(Guid FormFieldId, Guid FormSectionId, string SectionName, string QuestionLabel, QuestionType QuestionType, bool TemplateReportOnCoa);

    public Task<Result<SpecificationCoaReportingDto>> Get(Guid specificationId, AnalysisType analysisType) =>
        Get(Subject.Product, specificationId, analysisType);

    public async Task<Result<SpecificationCoaReportingDto>> Get(Subject subject, Guid specificationId, AnalysisType analysisType)
    {
        var target = await Load(subject, specificationId);
        if (target is null) return NotFound(subject);
        return await Read(target, analysisType);
    }

    public Task<Result<SpecificationCoaReportingDto>> Save(Guid specificationId, AnalysisType analysisType, SaveCoaReportingRequest request, Guid actorId) =>
        Save(Subject.Product, specificationId, analysisType, request, actorId);

    public async Task<Result<SpecificationCoaReportingDto>> Save(Subject subject, Guid specificationId, AnalysisType analysisType, SaveCoaReportingRequest request, Guid actorId)
    {
        var reason = request.Reason?.Trim() ?? "";
        if (reason.Length < 10) return Error.Validation("CoaReporting.Reason", "State the reason for changing the reported tests in at least 10 characters.");
        var target = await Load(subject, specificationId);
        if (target is null) return NotFound(subject);
        if (target.FormId(analysisType) is not { } formId)
            return Error.Validation("CoaReporting.NoTemplate", analysisType == AnalysisType.Microbial
                ? "Choose a microbial template on the specification before choosing its reported tests."
                : "Choose a chemical template on the specification before choosing its reported tests.");
        var overrides = request.Overrides ?? [];
        if (overrides.Any(item => item is null) || overrides.Select(item => item.FormFieldId).Distinct().Count() != overrides.Count)
            return Error.Validation("CoaReporting.Duplicate", "Choose each test at most once.");
        var fields = await Fields(formId, analysisType);
        var byId = fields.ToDictionary(field => field.FormFieldId);
        if (overrides.Any(item => !byId.ContainsKey(item.FormFieldId)))
            return Error.Validation("CoaReporting.Field", "A choice names a field that is not on the specification's template.");
        // Only differences from the template are stored, so later template changes still reach fields the specification left alone.
        var stored = overrides.Where(item => byId[item.FormFieldId].TemplateReportOnCoa != item.ReportOnCoa).ToList();
        var templateFieldIds = fields.Select(field => field.FormFieldId).ToList();
        if (target.EditKey is not { } key) return Denied(subject, null);

        await using var transaction = db.Database.IsRelational() && db.Database.CurrentTransaction is null
            ? await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable) : null;
        // Grants are read again immediately before the write.
        if (!(await CurrentPermissionSet.Load(db, actorId)).Contains(key)) return Denied(subject, key);

        var rows = subject == Subject.Product ? await ProductRows(target, analysisType, formId) : await MaterialRows(target, analysisType, formId);
        if (rows is null)
            return Error.Conflict("CoaReporting.Template", "Specification rows sharing this number name different templates. Save the specification first.");
        db.SpecificationCoaReportings.RemoveRange(await db.SpecificationCoaReportings
            .Where(item => (subject == Subject.Product ? item.ProductSpecificationId.HasValue && rows.Contains(item.ProductSpecificationId.Value)
                    : item.MaterialSpecificationId.HasValue && rows.Contains(item.MaterialSpecificationId.Value))
                && templateFieldIds.Contains(item.FormFieldId)).ToListAsync());
        var now = DateTime.UtcNow;
        foreach (var row in rows)
            db.SpecificationCoaReportings.AddRange(stored.Select(item => new SpecificationCoaReporting
            {
                ProductSpecificationId = subject == Subject.Product ? row : null,
                MaterialSpecificationId = subject == Subject.Material ? row : null,
                FormFieldId = item.FormFieldId, ReportOnCoa = item.ReportOnCoa,
                Reason = reason, SavedById = actorId, SavedAt = now, CreatedById = actorId,
            }));
        if (!(await CurrentPermissionSet.Load(db, actorId)).Contains(key)) return Denied(subject, key);
        await db.SaveChangesAsync();
        if (transaction is not null) await transaction.CommitAsync();
        db.ChangeTracker.Clear();
        var result = await Read(target, analysisType);
        // Record the reason even when the new choice matches the template and nothing is stored.
        if (stored.Count == 0) { result.Reason = reason; result.SavedAt = now; }
        return result;
    }

    /// <summary>The product specification rows saved together, with their approval cleared; null when they name different templates.</summary>
    private async Task<List<Guid>?> ProductRows(Target target, AnalysisType analysisType, Guid formId)
    {
        var specs = await db.ProductSpecifications.Where(spec => spec.Id == target.Id
            || (target.Number != null && spec.SpecificationNumber == target.Number)).ToListAsync();
        if (specs.Any(spec => (analysisType == AnalysisType.Microbial ? spec.MicrobialFormId : spec.FormId) != formId)) return null;
        foreach (var spec in specs) { spec.IsVerified = false; spec.VerifiedAt = null; spec.VerifiedById = null; }
        return specs.Select(spec => spec.Id).ToList();
    }

    /// <summary>The material specification rows saved together, with their approval cleared; null when they name different templates.</summary>
    private async Task<List<Guid>?> MaterialRows(Target target, AnalysisType analysisType, Guid formId)
    {
        var specs = await db.MaterialSpecifications.Where(spec => spec.Id == target.Id
            || (target.Number != null && spec.SpecificationNumber == target.Number)).ToListAsync();
        if (specs.Any(spec => (analysisType == AnalysisType.Microbial ? spec.MicrobialFormId : spec.FormId) != formId)) return null;
        foreach (var spec in specs) { spec.IsVerified = false; spec.VerifiedAt = null; spec.VerifiedById = null; }
        return specs.Select(spec => spec.Id).ToList();
    }

    private async Task<Target?> Load(Subject subject, Guid id) => subject == Subject.Product
        ? await db.ProductSpecifications.AsNoTracking().Where(spec => spec.Id == id)
            .Select(spec => new Target(Subject.Product, spec.Id, spec.SpecificationNumber, spec.FormId, spec.MicrobialFormId, null)).SingleOrDefaultAsync()
        : await db.MaterialSpecifications.AsNoTracking().Where(spec => spec.Id == id)
            .Select(spec => new Target(Subject.Material, spec.Id, spec.SpecificationNumber, spec.FormId, spec.MicrobialFormId, (MaterialKind?)spec.Material.Kind))
            .SingleOrDefaultAsync();

    private async Task<SpecificationCoaReportingDto> Read(Target target, AnalysisType analysisType)
    {
        var formId = target.FormId(analysisType);
        var fields = formId.HasValue ? await Fields(formId.Value, analysisType) : [];
        var fieldIds = fields.Select(field => field.FormFieldId).ToList();
        var overrides = await db.SpecificationCoaReportings.AsNoTracking()
            .Where(item => (target.Subject == Subject.Product ? item.ProductSpecificationId == target.Id : item.MaterialSpecificationId == target.Id)
                && fieldIds.Contains(item.FormFieldId)).ToListAsync();
        var byField = overrides.ToDictionary(item => item.FormFieldId, item => item.ReportOnCoa);
        var latest = overrides.OrderByDescending(item => item.SavedAt).FirstOrDefault();
        return new SpecificationCoaReportingDto
        {
            SpecificationId = target.Id,
            AnalysisType = analysisType,
            FormId = formId,
            Fields = fields.Select(field => new CoaReportingFieldDto
            {
                FormFieldId = field.FormFieldId, FormSectionId = field.FormSectionId, SectionName = field.SectionName,
                QuestionLabel = field.QuestionLabel, QuestionType = field.QuestionType, TemplateReportOnCoa = field.TemplateReportOnCoa,
                Override = byField.TryGetValue(field.FormFieldId, out var value) ? value : null,
                ReportOnCoa = byField.TryGetValue(field.FormFieldId, out var chosen) ? chosen : field.TemplateReportOnCoa,
            }).ToList(),
            Reason = latest?.Reason,
            SavedAt = latest?.SavedAt,
        };
    }

    private async Task<List<Field>> Fields(Guid formId, AnalysisType analysisType)
    {
        var rows = await db.FormFields.AsNoTracking()
            .Where(field => field.FormSection.FormId == formId && field.DeletedAt == null && field.FormSection.DeletedAt == null
                && (field.FormSection.AnalysisType == null || field.FormSection.AnalysisType == analysisType))
            .OrderBy(field => field.FormSection.Order).ThenBy(field => field.Rank)
            .Select(field => new { field.Id, field.FormSectionId, Section = field.FormSection.Name, field.Question.Label, field.Question.Type, field.ReportOnCoa })
            .ToListAsync();
        return rows.Select(row => new Field(row.Id, row.FormSectionId, FormErrors.PlainName(row.Section), row.Label, row.Type, row.ReportOnCoa)).ToList();
    }

    private static Error Denied(Subject subject, string? key) => Error.Forbidden("CoaReporting.Permission", key is null
        ? "This specification's material kind has no certificate reporting permission."
        : $"You need {key} to change the tests reported on the certificate.");

    private static Error NotFound(Subject subject) => Error.NotFound("CoaReporting.Specification",
        subject == Subject.Product ? "Product specification not found." : "Material specification not found.");
}
