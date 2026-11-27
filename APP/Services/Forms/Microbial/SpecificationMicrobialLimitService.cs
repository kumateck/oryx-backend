using APP.Services.Authorization;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Materials;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

#nullable enable
namespace APP.Services.Forms.Microbial;

/// <summary>
/// The typed microbial limits of a specification. Limits sit on the result fields of the specification's microbial template.
/// Specification rows that share a number are one document: saving replaces the limits on every row and clears their approval.
/// </summary>
public sealed class SpecificationMicrobialLimitService(ApplicationDbContext db)
{
    public enum Subject { Material, Product }

    private sealed record Target(Guid Id, string? Number, Guid? MicrobialFormId, MaterialKind? Kind);

    public async Task<Result<SpecificationMicrobialLimitsDto>> Get(Subject subject, Guid id)
    {
        var target = await Load(subject, id);
        if (target is null) return NotFound(subject);
        return await Read(subject, target);
    }

    public async Task<Result<SpecificationMicrobialLimitsDto>> Save(Subject subject, Guid id, SaveMicrobialLimitsRequest request, Guid actorId)
    {
        var reason = request.Reason?.Trim() ?? "";
        if (reason.Length < 10) return Error.Validation("MicrobialLimit.Reason", "State the reason for changing the limits in at least 10 characters.");
        var target = await Load(subject, id);
        if (target is null) return NotFound(subject);
        if (!target.MicrobialFormId.HasValue)
            return Error.Validation("MicrobialLimit.NoTemplate", "Choose a microbial template on the specification before setting microbial limits.");
        var limits = request.Limits ?? [];
        if (limits.Select(limit => limit.FormFieldId).Distinct().Count() != limits.Count)
            return Error.Validation("MicrobialLimit.Duplicate", "Set one limit per result field.");
        var fields = await Fields(target.MicrobialFormId.Value);
        foreach (var limit in limits)
        {
            var field = fields.SingleOrDefault(item => item.FormFieldId == limit.FormFieldId);
            if (field is null) return Error.Validation("MicrobialLimit.Field", "A limit names a field that is not a result field of the specification's microbial template.");
            var valid = MicrobialLimits.Validate(limit, field.QuestionType, field.Unit);
            if (valid.IsFailure) return valid.Errors;
        }

        await using var transaction = db.Database.IsRelational() && db.Database.CurrentTransaction is null
            ? await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable) : null;
        // Grants are read again immediately before the write.
        var key = subject == Subject.Product
            ? QcDocumentPermissions.ProductKey(QcDocument.ProductSpecification, QcTask.EditMicrobialLimits)
            : QcDocumentPermissions.MaterialKey(QcDocument.MaterialSpecification, QcTask.EditMicrobialLimits, target.Kind ?? MaterialKind.Raw);
        if (key is null || !(await CurrentPermissionSet.Load(db, actorId)).Contains(key))
            return Error.Forbidden("MicrobialLimit.Permission", $"You need {key} to change these limits.");

        var now = DateTime.UtcNow;
        List<Guid> rows;
        if (subject == Subject.Material)
        {
            var specs = await db.MaterialSpecifications.Where(spec => spec.Id == id
                || (target.Number != null && spec.SpecificationNumber == target.Number)).ToListAsync();
            if (specs.Any(spec => spec.MicrobialFormId != target.MicrobialFormId))
                return Error.Conflict("MicrobialLimit.Template", "Specification rows sharing this number name different microbial templates. Save the specification first.");
            foreach (var spec in specs) { spec.IsVerified = false; spec.VerifiedAt = null; spec.VerifiedById = null; }
            rows = specs.Select(spec => spec.Id).ToList();
            db.SpecificationMicrobialLimits.RemoveRange(await db.SpecificationMicrobialLimits.Where(limit => limit.MaterialSpecificationId != null && rows.Contains(limit.MaterialSpecificationId.Value)).ToListAsync());
        }
        else
        {
            var specs = await db.ProductSpecifications.Where(spec => spec.Id == id
                || (target.Number != null && spec.SpecificationNumber == target.Number)).ToListAsync();
            if (specs.Any(spec => spec.MicrobialFormId != target.MicrobialFormId))
                return Error.Conflict("MicrobialLimit.Template", "Specification rows sharing this number name different microbial templates. Save the specification first.");
            foreach (var spec in specs) { spec.IsVerified = false; spec.VerifiedAt = null; spec.VerifiedById = null; }
            rows = specs.Select(spec => spec.Id).ToList();
            db.SpecificationMicrobialLimits.RemoveRange(await db.SpecificationMicrobialLimits.Where(limit => limit.ProductSpecificationId != null && rows.Contains(limit.ProductSpecificationId.Value)).ToListAsync());
        }
        foreach (var row in rows)
            db.SpecificationMicrobialLimits.AddRange(limits.Select((limit, index) => new SpecificationMicrobialLimit
            {
                MaterialSpecificationId = subject == Subject.Material ? row : null,
                ProductSpecificationId = subject == Subject.Product ? row : null,
                FormFieldId = limit.FormFieldId,
                Kind = limit.Kind,
                Value = limit.Kind == MicrobialLimitKind.NotMoreThan ? limit.Value : null,
                Unit = limit.Kind == MicrobialLimitKind.NotMoreThan ? limit.Unit : null,
                Organism = limit.Kind == MicrobialLimitKind.Absent ? limit.Organism : null,
                Quantity = limit.Kind == MicrobialLimitKind.Absent ? limit.Quantity : null,
                QuantityUnit = limit.Kind == MicrobialLimitKind.Absent ? limit.QuantityUnit : null,
                DisplayOrder = index,
                Reason = reason,
                SavedById = actorId,
                SavedAt = now,
            }));
        if (!(await CurrentPermissionSet.Load(db, actorId)).Contains(key))
            return Error.Forbidden("MicrobialLimit.Permission", $"You need {key} to change these limits.");
        await db.SaveChangesAsync();
        if (transaction is not null) await transaction.CommitAsync();
        db.ChangeTracker.Clear();
        return await Read(subject, target);
    }

    /// <summary>The limits that apply to a microbial worksheet response, from the specification of the response's material or product stage.</summary>
    public static async Task<List<SpecificationMicrobialLimit>> ForResponse(ApplicationDbContext db, Response response)
    {
        if (response.MaterialSamplingId.HasValue || response.MaterialBatchId.HasValue)
        {
            var batchId = response.MaterialBatchId ?? await db.MaterialSamplings.AsNoTracking().Where(item => item.Id == response.MaterialSamplingId)
                .Select(item => (Guid?)item.MaterialBatchId).SingleOrDefaultAsync();
            var materialId = await db.MaterialBatches.AsNoTracking().Where(item => item.Id == batchId).Select(item => (Guid?)item.MaterialId).SingleOrDefaultAsync();
            var spec = await db.MaterialSpecifications.AsNoTracking().Where(item => item.MaterialId == materialId && item.MicrobialFormId == response.FormId)
                .Select(item => (Guid?)item.Id).FirstOrDefaultAsync();
            return spec is null ? [] : await db.SpecificationMicrobialLimits.AsNoTracking().Where(limit => limit.MaterialSpecificationId == spec).OrderBy(limit => limit.DisplayOrder).ToListAsync();
        }
        if (response.BatchManufacturingRecordId.HasValue)
        {
            var atr = await db.AnalyticalTestRequests.AsNoTracking()
                .Where(item => item.BatchManufacturingRecordId == response.BatchManufacturingRecordId && item.ProductionActivityStepId == response.ProductionActivityStepId)
                .OrderByDescending(item => item.CreatedAt).Select(item => new { item.ProductionScheduleProduct.ProductId, item.Stage }).FirstOrDefaultAsync();
            if (atr is null) return [];
            var spec = await db.ProductSpecifications.AsNoTracking().Where(item => item.ProductId == atr.ProductId && item.TestStage == atr.Stage && item.MicrobialFormId == response.FormId)
                .Select(item => (Guid?)item.Id).FirstOrDefaultAsync();
            return spec is null ? [] : await db.SpecificationMicrobialLimits.AsNoTracking().Where(limit => limit.ProductSpecificationId == spec).OrderBy(limit => limit.DisplayOrder).ToListAsync();
        }
        return [];
    }

    private async Task<Target?> Load(Subject subject, Guid id) => subject == Subject.Material
        ? await db.MaterialSpecifications.AsNoTracking().Where(spec => spec.Id == id)
            .Select(spec => new Target(spec.Id, spec.SpecificationNumber, spec.MicrobialFormId, spec.Material.Kind)).SingleOrDefaultAsync()
        : await db.ProductSpecifications.AsNoTracking().Where(spec => spec.Id == id)
            .Select(spec => new Target(spec.Id, spec.SpecificationNumber, spec.MicrobialFormId, null)).SingleOrDefaultAsync();

    private async Task<SpecificationMicrobialLimitsDto> Read(Subject subject, Target target)
    {
        var limits = await db.SpecificationMicrobialLimits.AsNoTracking()
            .Where(limit => subject == Subject.Material ? limit.MaterialSpecificationId == target.Id : limit.ProductSpecificationId == target.Id)
            .OrderBy(limit => limit.DisplayOrder).ToListAsync();
        return new SpecificationMicrobialLimitsDto
        {
            SpecificationId = target.Id,
            MicrobialFormId = target.MicrobialFormId,
            Fields = target.MicrobialFormId.HasValue ? await Fields(target.MicrobialFormId.Value) : [],
            Limits = limits.Select(limit => new MicrobialLimitDto
            {
                FormFieldId = limit.FormFieldId, Kind = limit.Kind, Value = limit.Value, Unit = limit.Unit, Organism = limit.Organism,
                Quantity = limit.Quantity, QuantityUnit = limit.QuantityUnit, Text = MicrobialLimits.Text(limit),
            }).ToList(),
            Organisms = MicrobialOrganisms.Names.ToDictionary(),
            Reason = limits.FirstOrDefault()?.Reason,
            SavedAt = limits.FirstOrDefault()?.SavedAt,
        };
    }

    private async Task<List<MicrobialLimitFieldDto>> Fields(Guid formId)
    {
        var rows = await db.FormFields.AsNoTracking()
            .Where(field => field.FormSection.FormId == formId && field.DeletedAt == null && field.FormSection.DeletedAt == null
                && (field.Question.Type == QuestionType.CfuCalculation || field.Question.Type == QuestionType.GrowthObservation))
            .OrderBy(field => field.FormSection.Order).ThenBy(field => field.Rank)
            .Select(field => new
            {
                field.Id, Section = field.FormSection.Name, field.Question.Label, field.Question.Type,
                Unit = field.Question.Options.Where(option => option.DeletedAt == null).Select(option => option.Name).FirstOrDefault(),
            }).ToListAsync();
        return rows.Select(row => new MicrobialLimitFieldDto
        {
            FormFieldId = row.Id, SectionName = FormErrors.PlainName(row.Section), QuestionLabel = row.Label, QuestionType = row.Type,
            Unit = row.Type == QuestionType.CfuCalculation ? row.Unit?.Trim() : null,
        }).ToList();
    }

    private static Error NotFound(Subject subject) => Error.NotFound("MicrobialLimit.Specification",
        subject == Subject.Product ? "Product specification not found." : "Material specification not found.");
}
