using System.Data;
using System.Text.Json;
using APP.Services.Authorization;
using APP.Services.Formulas;
using APP.Utils;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.ProductSpecifications;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

#nullable enable
namespace APP.Services.Forms;

public sealed class ProductAssayReportingService(ApplicationDbContext db)
{
    internal static List<AssayReportingRule> Rules(string? json) => string.IsNullOrWhiteSpace(json) ? [] : JsonSerializer.Deserialize<List<AssayReportingRule>>(json)!;
    internal static Error Invalid(string message) => Error.Conflict("AssayReporting.Configuration", message);
    private async Task<bool> Allowed(Guid actorId, string key) => (await CurrentPermissionSet.Load(db, actorId)).Contains(key);

    public async Task<Result<AssayReportingConfigurationDto>> Get(Guid id, Guid actorId)
    {
        if (!await Allowed(actorId, PermissionKeys.CanViewProductAssayReporting)) return Error.Forbidden("AssayReporting.Permission", "You cannot view product assay reporting settings.");
        var specification = await db.ProductSpecifications.AsNoTracking().Include(item => item.Product).SingleOrDefaultAsync(item => item.Id == id);
        return specification is null ? Error.NotFound("AssayReporting.Specification", "The product specification was not found.") : await Describe(specification);
    }

    private async Task<AssayReportingConfigurationDto> Describe(ProductSpecification specification)
    {
        var fields = await db.FormFields.AsNoTracking().Include(field => field.FormSection).Include(field => field.Question).ThenInclude(question => question.Options)
            .Where(field => field.FormSection.FormId == specification.FormId && field.Question.Type == QuestionType.Formula).OrderBy(field => field.FormSection.Order).ThenBy(field => field.Rank).ThenBy(field => field.Id).ToListAsync();
        var result = new List<AssayReportingFieldDto>();
        foreach (var field in fields)
        {
            try
            {
                var source = await AssayFormulaSource.Load(db, field);
                result.Add(new(field.Id, field.FormSection.Name, field.Question.Label, field.FormSection.Description, source.Hash, source.Outputs, null!));
            }
            catch (Exception error) when (error is ArgumentException or JsonException or InvalidOperationException)
            { result.Add(new(field.Id, field.FormSection.Name, field.Question.Label, field.FormSection.Description, "", [], "This formula needs a supported approved revision before COA reporting can be configured.")); }
        }
        var rules = Rules(specification.AssayReportingRulesJson);
        var token = LegacyFormulaRepairDefinition.Hash(JsonSerializer.Serialize(new { specification.Id, specification.ProductId, specification.FormId,
            specification.RevisionNumber, specification.IsVerified, result, rules }));
        return new(specification.Id, specification.ProductId, specification.Product?.Name ?? "", (int)specification.TestStage,
            specification.SpecificationNumber, token, result, rules);
    }

    private async Task RestartPendingReviews(ProductSpecification specification, List<Guid> fieldIds, Guid actorId, string reason)
    {
        var reviewNote = "New review round after assay reporting changed: " + reason;
        if (reviewNote.Length > 1000) reviewNote = reviewNote[..997] + "..."; // The full reason stays on the specification revision.
        var responses = await db.Responses.Include(response => response.Approvals)
            .Where(response => !response.Approved && !response.Rejected && response.CheckedAt.HasValue
                && response.FormResponses.Any(answer => fieldIds.Contains(answer.FormFieldId))
                && db.AnalyticalTestRequests.Any(atr => atr.BatchManufacturingRecordId == response.BatchManufacturingRecordId
                    && atr.ProductionActivityStepId == response.ProductionActivityStepId && atr.Stage == specification.TestStage
                    && atr.ProductionScheduleProduct.ProductId == specification.ProductId)).ToListAsync();
        foreach (var response in responses)
        {
            var current = APP.Repository.ResponseApprovalRoundManager.Current(response.Approvals);
            if (current.Count == 0) continue;
            var round = checked(current.Max(stage => stage.ApprovalRound) + 1);
            var now = DateTime.UtcNow;
            var stages = current.Select(stage => new ResponseApproval {
                Id = Guid.NewGuid(), ResponseId = response.Id, ApprovalId = stage.ApprovalId, ApprovalRound = round,
                Required = stage.Required, Order = stage.Order, UserId = stage.UserId, RoleId = stage.RoleId,
                FormulaSubmissionSetId = stage.FormulaSubmissionSetId, CreatedAt = now,
                Comments = reviewNote
            }).ToList();
            APP.Repository.ResponseApprovalRoundManager.ActivateEligible(stages, now);
            response.LastUpdatedById = actorId;
            db.ResponseApprovals.AddRange(stages);
        }
    }

    public async Task<Result<AssayReportingConfigurationDto>> Save(Guid id, SaveAssayReportingRequest request, Guid actorId)
    {
        await using var transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable) : null;
        if (!await Allowed(actorId, PermissionKeys.CanEditProductAssayReporting)) return Error.Forbidden("AssayReporting.Permission", "You cannot edit product assay reporting settings.");
        var specification = await db.ProductSpecifications.Include(item => item.Product).SingleOrDefaultAsync(item => item.Id == id);
        if (specification is null) return Error.NotFound("AssayReporting.Specification", "The product specification was not found.");
        var current = await Describe(specification);
        if (request.ExpectedToken != current.Token) return Invalid("The product specification, formula or reporting settings changed. Reload before saving.");
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length > 2000) return Invalid("Enter a reason of 1–2000 characters for this reporting change.");
        if (request.Rules is null || request.Rules.Count > 100 || request.Rules.Select(rule => (rule.FormFieldId, rule.ResultKey)).Distinct().Count() != request.Rules.Count)
            return Invalid("Choose each assay output at most once (up to 100 reporting rules).");
        foreach (var rule in request.Rules)
        {
            // Existing stale rules remain visible and block certificate submission. Permit removing them one at a time.
            if (current.Rules.Contains(rule)) continue;
            var field = current.Fields.SingleOrDefault(field => field.FormFieldId == rule.FormFieldId);
            if (field is null || field.DefinitionHash != rule.DefinitionHash || !field.Outputs.Any(output => output.Key == rule.ResultKey && output.CanConvert))
                return Invalid("Select an output of this product's current assay formula. Reload after changing the template or formula.");
            if (rule.NominalConcentration is <= 0 or > 1000000 || rule.Minimum < 0 || rule.Maximum < rule.Minimum || rule.Maximum > 1000000 || rule.DecimalPlaces is < 0 or > 8)
                return Invalid("Enter a positive nominal % w/v concentration, valid nonnegative specification limits and 0–8 decimal places.");
        }
        var orderedRules = request.Rules.OrderBy(rule => rule.FormFieldId).ThenBy(rule => rule.ResultKey).ToList();
        if (!current.Rules.OrderBy(rule => rule.FormFieldId).ThenBy(rule => rule.ResultKey).SequenceEqual(orderedRules))
            await RestartPendingReviews(specification, current.Rules.Concat(request.Rules).Select(rule => rule.FormFieldId).Distinct().ToList(), actorId, request.Reason.Trim());
        if (!await Allowed(actorId, PermissionKeys.CanEditProductAssayReporting)) return Error.Forbidden("AssayReporting.Permission", "Your reporting permission changed. Reload before saving.");
        specification.AssayReportingRulesJson = JsonSerializer.Serialize(request.Rules.OrderBy(rule => rule.FormFieldId).ThenBy(rule => rule.ResultKey));
        specification.LastUpdatedById = actorId;
        specification.RevisionReason = request.Reason.Trim();
        specification.IsVerified = false; specification.VerifiedAt = null; specification.VerifiedById = null;
        await db.SaveChangesAsync();
        if (transaction is not null) await transaction.CommitAsync();
        return await Describe(specification);
    }
}
