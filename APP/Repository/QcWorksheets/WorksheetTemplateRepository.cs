using System.Text.Json;
using APP.IRepository;
using APP.Services.QcWorksheets;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.QcWorksheets;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository.QcWorksheets;

public class WorksheetTemplateRepository(
    ApplicationDbContext context,
    IMapper mapper,
    IQcSignatureService signatureService,
    IQcReauthContext reauthContext,
    IApprovalRepository approvalRepository) : IWorksheetTemplateRepository
{
    private const string ModelType = QcWorksheetModelTypes.WorksheetTemplate;

    // -----------------------------------------------------------------------
    // Reads
    // -----------------------------------------------------------------------

    public async Task<Result<Paginateable<IEnumerable<WorksheetTemplateSummaryDto>>>> GetTemplates(
        int page, int pageSize, string searchQuery, QcDocumentStatus? status)
    {
        var query = context.QcWorksheetTemplates
            .Include(item => item.CreatedBy)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            var term = searchQuery.Trim().ToLower();
            query = query.Where(item =>
                item.Code.ToLower().Contains(term)
                || item.Name.ToLower().Contains(term)
                || (item.Department != null && item.Department.ToLower().Contains(term)));
        }

        if (status.HasValue)
            query = query.Where(item => item.Status == status.Value);

        return await PaginationHelper.GetPaginatedResultAsync(
            query.OrderByDescending(item => item.CreatedAt),
            page,
            pageSize,
            ToSummaryDto);
    }

    public async Task<Result<WorksheetTemplateDetailDto>> GetTemplate(Guid id)
    {
        var template = await LoadDetail(id);
        return template is null
            ? Result.Failure<WorksheetTemplateDetailDto>(QcWorksheetErrors.TemplateNotFound(id))
            : Result.Success(ToDetailDto(template));
    }

    // -----------------------------------------------------------------------
    // Authoring
    // -----------------------------------------------------------------------

    public async Task<Result<WorksheetTemplateDetailDto>> CreateTemplate(
        CreateWorksheetTemplateRequest request, Guid userId)
    {
        var validation = ValidateStructure(request);
        if (!validation.IsSuccess)
            return Result.Failure<WorksheetTemplateDetailDto>(validation.Error);

        if (request.StpId.HasValue
            && !await context.QcStandardTestProcedures.AnyAsync(item => item.Id == request.StpId.Value))
            return Result.Failure<WorksheetTemplateDetailDto>(
                QcWorksheetErrors.ReferencedStpNotFound(request.StpId.Value));

        var template = new WorksheetTemplate
        {
            Id = Guid.NewGuid(),
            Code = request.Code?.Trim(),
            Name = request.Name?.Trim(),
            Department = request.Department?.Trim(),
            StpId = request.StpId,
            Category = request.Category,
            Version = 1,
            Status = QcDocumentStatus.Draft,
            Approved = false,
            CreatedAt = DateTime.UtcNow,
            CreatedById = userId,
            Sections = BuildSections(request.Sections, userId)
        };

        context.QcWorksheetTemplates.Add(template);
        await context.SaveChangesAsync();

        return await GetTemplate(template.Id);
    }

    public async Task<Result<WorksheetTemplateDetailDto>> UpdateTemplate(
        Guid id, UpdateWorksheetTemplateRequest request, Guid userId)
    {
        var template = await LoadDetail(id);
        if (template is null)
            return Result.Failure<WorksheetTemplateDetailDto>(QcWorksheetErrors.TemplateNotFound(id));

        if (template.Status is not (QcDocumentStatus.Draft or QcDocumentStatus.UnderReview))
            return Result.Failure<WorksheetTemplateDetailDto>(
                QcWorksheetErrors.NotEditableInStatus(template.Status));

        var validation = ValidateStructure(request);
        if (!validation.IsSuccess)
            return Result.Failure<WorksheetTemplateDetailDto>(validation.Error);

        template.Code = request.Code?.Trim();
        template.Name = request.Name?.Trim();
        template.Department = request.Department?.Trim();
        template.StpId = request.StpId;
        template.Category = request.Category;
        template.UpdatedAt = DateTime.UtcNow;
        template.LastUpdatedById = userId;

        if (template.Status == QcDocumentStatus.UnderReview)
        {
            template.Status = QcDocumentStatus.Draft;
            template.Approved = false;
        }

        MergeSections(template, request.Sections, userId);

        await context.SaveChangesAsync();
        return await GetTemplate(id);
    }

    public async Task<Result<WorksheetTemplateDetailDto>> CreateNewVersion(Guid id, Guid userId)
    {
        var source = await LoadDetail(id);
        if (source is null)
            return Result.Failure<WorksheetTemplateDetailDto>(QcWorksheetErrors.TemplateNotFound(id));

        if (source.Status != QcDocumentStatus.Effective)
            return Result.Failure<WorksheetTemplateDetailDto>(
                QcWorksheetErrors.NewVersionRequiresEffective(source.Status));

        var draft = new WorksheetTemplate
        {
            Id = Guid.NewGuid(),
            Code = source.Code,
            Name = source.Name,
            Department = source.Department,
            StpId = source.StpId,
            Category = source.Category,
            Version = source.Version + 1,
            SupersedesId = source.Id,
            Status = QcDocumentStatus.Draft,
            Approved = false,
            CreatedAt = DateTime.UtcNow,
            CreatedById = userId,
            Sections = source.Sections
                .OrderBy(section => section.Order)
                .Select(section => new WorksheetSection
                {
                    Id = Guid.NewGuid(),
                    Order = section.Order,
                    Name = section.Name,
                    InstrumentId = section.InstrumentId,
                    CreatedAt = DateTime.UtcNow,
                    CreatedById = userId,
                    Fields = section.Fields
                        .OrderBy(field => field.Order)
                        .Select(field => CloneField(field, userId))
                        .ToList()
                })
                .ToList()
        };

        context.QcWorksheetTemplates.Add(draft);
        await context.SaveChangesAsync();

        return await GetTemplate(draft.Id);
    }

    // -----------------------------------------------------------------------
    // Lifecycle
    // -----------------------------------------------------------------------

    public async Task<Result<WorksheetTemplateDetailDto>> SubmitForReview(Guid id, Guid userId)
    {
        var template = await context.QcWorksheetTemplates.SingleOrDefaultAsync(item => item.Id == id);
        if (template is null)
            return Result.Failure<WorksheetTemplateDetailDto>(QcWorksheetErrors.TemplateNotFound(id));

        if (template.Status != QcDocumentStatus.Draft)
            return Result.Failure<WorksheetTemplateDetailDto>(
                QcWorksheetErrors.SubmitRequiresDraft(template.Status));

        if (!await HasConfiguredApprovalChain())
            return Result.Failure<WorksheetTemplateDetailDto>(
                QcWorksheetErrors.NoApprovalWorkflowConfigured(ModelType));

        template.Status = QcDocumentStatus.UnderReview;
        template.UpdatedAt = DateTime.UtcNow;
        template.LastUpdatedById = userId;
        await context.SaveChangesAsync();

        await approvalRepository.CreateInitialApprovalsAsync(ModelType, id);

        return await GetTemplate(id);
    }

    public async Task<Result<WorksheetTemplateDetailDto>> Approve(
        Guid id, QcApprovalRequest request, Guid userId, List<Guid> roleIds)
    {
        var template = await context.QcWorksheetTemplates.SingleOrDefaultAsync(item => item.Id == id);
        if (template is null)
            return Result.Failure<WorksheetTemplateDetailDto>(QcWorksheetErrors.TemplateNotFound(id));

        if (template.Status != QcDocumentStatus.UnderReview)
            return Result.Failure<WorksheetTemplateDetailDto>(
                QcWorksheetErrors.ApproveRequiresUnderReview(template.Status));

        var signed = await signatureService.SignAndApproveAsync(
            ModelType, id, userId, roleIds, request.Password, request.Comments);

        return !signed.IsSuccess
            ? Result.Failure<WorksheetTemplateDetailDto>(signed.Error)
            : await GetTemplate(id);
    }

    public async Task<Result<WorksheetTemplateDetailDto>> Reject(
        Guid id, QcApprovalRequest request, Guid userId, List<Guid> roleIds)
    {
        var template = await context.QcWorksheetTemplates.SingleOrDefaultAsync(item => item.Id == id);
        if (template is null)
            return Result.Failure<WorksheetTemplateDetailDto>(QcWorksheetErrors.TemplateNotFound(id));

        if (template.Status != QcDocumentStatus.UnderReview)
            return Result.Failure<WorksheetTemplateDetailDto>(
                QcWorksheetErrors.ApproveRequiresUnderReview(template.Status));

        var signed = await signatureService.SignAndRejectAsync(
            ModelType, id, userId, roleIds, request.Password, request.Comments);

        return !signed.IsSuccess
            ? Result.Failure<WorksheetTemplateDetailDto>(signed.Error)
            : await GetTemplate(id);
    }

    public async Task<Result<WorksheetTemplateDetailDto>> MakeEffective(Guid id, Guid userId)
    {
        var template = await context.QcWorksheetTemplates.SingleOrDefaultAsync(item => item.Id == id);
        if (template is null)
            return Result.Failure<WorksheetTemplateDetailDto>(QcWorksheetErrors.TemplateNotFound(id));

        if (template.Status != QcDocumentStatus.Approved)
            return Result.Failure<WorksheetTemplateDetailDto>(
                QcWorksheetErrors.MakeEffectiveRequiresApproved(template.Status));

        template.Status = QcDocumentStatus.Effective;
        template.EffectiveDate = DateTime.UtcNow;
        template.UpdatedAt = DateTime.UtcNow;
        template.LastUpdatedById = userId;

        if (template.SupersedesId.HasValue)
        {
            var superseded = await context.QcWorksheetTemplates
                .SingleOrDefaultAsync(item => item.Id == template.SupersedesId.Value);

            if (superseded is { Status: QcDocumentStatus.Effective })
            {
                superseded.Status = QcDocumentStatus.Superseded;
                superseded.UpdatedAt = DateTime.UtcNow;
                superseded.LastUpdatedById = userId;
            }
        }

        await context.SaveChangesAsync();
        return await GetTemplate(id);
    }

    public async Task<Result<WorksheetTemplateDetailDto>> Supersede(
        Guid id, QcSupersedeRequest request, Guid userId)
    {
        var template = await context.QcWorksheetTemplates.SingleOrDefaultAsync(item => item.Id == id);
        if (template is null)
            return Result.Failure<WorksheetTemplateDetailDto>(QcWorksheetErrors.TemplateNotFound(id));

        if (template.Status != QcDocumentStatus.Effective)
            return Result.Failure<WorksheetTemplateDetailDto>(
                QcWorksheetErrors.SupersedeRequiresEffective(template.Status));

        if (string.IsNullOrWhiteSpace(request.Comments))
            return Result.Failure<WorksheetTemplateDetailDto>(QcWorksheetErrors.ReasonForChangeRequired);

        var approval = await context.Approvals.FirstOrDefaultAsync(item => item.ItemType == ModelType);
        if (approval is null)
            return Result.Failure<WorksheetTemplateDetailDto>(
                QcWorksheetErrors.NoApprovalWorkflowConfigured(ModelType));

        var verified = await signatureService.VerifyAsync(userId, request.Password);
        if (!verified.IsSuccess)
            return Result.Failure<WorksheetTemplateDetailDto>(verified.Error);

        var recorded = await QcApprovalHandler.RecordSignedActionAsync(
            context, reauthContext, ModelType, id, approval.Id, userId, request.Comments);

        if (!recorded.IsSuccess)
            return Result.Failure<WorksheetTemplateDetailDto>(recorded.Error);

        template.Status = QcDocumentStatus.Superseded;
        template.UpdatedAt = DateTime.UtcNow;
        template.LastUpdatedById = userId;
        await context.SaveChangesAsync();

        return await GetTemplate(id);
    }

    // -----------------------------------------------------------------------
    // Validation
    // -----------------------------------------------------------------------

    private static Result ValidateStructure(CreateWorksheetTemplateRequest request)
    {
        var sections = request.Sections ?? [];
        if (sections.Count == 0)
            return QcWorksheetErrors.TemplateHasNoSections;

        var fields = sections.SelectMany(section => section.Fields ?? []).ToList();

        // FieldKey uniqueness is worksheet-scoped, not section-scoped, because
        // CalculatedValue formulas reference field keys across the whole worksheet.
        var duplicate = fields
            .GroupBy(field => field.FieldKey?.Trim(), StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicate is not null)
            return QcWorksheetErrors.DuplicateFieldKey(duplicate.Key);

        var knownKeys = fields
            .Select(field => field.FieldKey?.Trim())
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var tableKeys = fields
            .Where(field => field.Type == WorksheetFieldType.Table)
            .Select(field => field.FieldKey?.Trim())
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var field in fields)
        {
            if (field.Mode == WorksheetFieldMode.Constant
                && field.Type != WorksheetFieldType.Heading
                && string.IsNullOrWhiteSpace(field.ConstantValue))
                return QcWorksheetErrors.ConstantValueRequired(field.FieldKey);

            var options = WorksheetFieldOptions.Validate(field);
            if (!options.IsSuccess)
                return options;

            var columns = WorksheetTableColumns.Validate(field);
            if (!columns.IsSuccess)
                return columns;

            var columnFormulas = WorksheetColumnFormulas.Validate(field, knownKeys, tableKeys, FindTableColumn);
            if (!columnFormulas.IsSuccess)
                return columnFormulas;

            var formulaRequired = field.Type is WorksheetFieldType.CalculatedValue
                or WorksheetFieldType.CfuCalculation;

            if (!formulaRequired)
                continue;

            if (string.IsNullOrWhiteSpace(field.FormulaExpression))
                return QcWorksheetErrors.InvalidFormula(field.FieldKey, "a formula is required for this field type");

            var analysis = QcFormulaEvaluator.Analyze(field.FormulaExpression);
            if (!analysis.IsValid)
                return QcWorksheetErrors.InvalidFormula(field.FieldKey, analysis.Error);

            foreach (var reference in analysis.ScalarFieldKeys)
            {
                if (string.Equals(reference, field.FieldKey?.Trim(), StringComparison.OrdinalIgnoreCase))
                    return QcWorksheetErrors.InvalidFormula(field.FieldKey, "a formula cannot reference its own field");

                if (!knownKeys.Contains(reference))
                    return QcWorksheetErrors.InvalidFormula(
                        field.FieldKey, $"it references '{reference}', which is not a field in this template");
            }

            foreach (var reference in analysis.TableReferences)
            {
                var check = WorksheetColumnFormulas.ValidateTableReference(
                    field.FieldKey, reference, knownKeys, tableKeys, FindTableColumn);
                if (!check.IsSuccess)
                    return check;
            }
        }

        return Result.Success();

        bool? FindTableColumn(string tableKey, string columnKey) => FindColumn(fields, tableKey, columnKey);
    }

    /// <summary>
    /// Returns true when the column is found, false when the table's columns are known and
    /// it is absent, and null when the column definitions cannot be read (in which case the
    /// reference is not rejected).
    /// </summary>
    private static bool? FindColumn(
        List<CreateWorksheetFieldRequest> fields, string tableKey, string columnKey)
    {
        var table = fields.FirstOrDefault(field =>
            string.Equals(field.FieldKey?.Trim(), tableKey, StringComparison.OrdinalIgnoreCase));

        if (string.IsNullOrWhiteSpace(table?.ColumnDefinitions))
            return null;

        if (WorksheetRowHeaders.IsHeaderColumn(table.ColumnDefinitions, columnKey))
            return false;

        try
        {
            using var parsed = JsonDocument.Parse(table.ColumnDefinitions);
            if (parsed.RootElement.ValueKind != JsonValueKind.Array)
                return null;

            foreach (var column in parsed.RootElement.EnumerateArray())
            {
                foreach (var property in new[] { "key", "columnKey", "label" })
                {
                    if (column.TryGetProperty(property, out var value)
                        && value.ValueKind == JsonValueKind.String
                        && string.Equals(value.GetString()?.Trim(), columnKey, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }

            return false;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    // -----------------------------------------------------------------------
    // Structure building / merging
    // -----------------------------------------------------------------------

    private static List<WorksheetSection> BuildSections(
        List<CreateWorksheetSectionRequest> sections, Guid userId) =>
        (sections ?? [])
        .OrderBy(section => section.Order)
        .Select((section, index) => new WorksheetSection
        {
            Id = Guid.NewGuid(),
            Order = section.Order == 0 ? index + 1 : section.Order,
            Name = section.Name?.Trim(),
            InstrumentId = section.InstrumentId,
            CreatedAt = DateTime.UtcNow,
            CreatedById = userId,
            Fields = (section.Fields ?? [])
                .OrderBy(field => field.Order)
                .Select((field, fieldIndex) => NewField(field, fieldIndex, userId))
                .ToList()
        })
        .ToList();

    private static WorksheetField NewField(CreateWorksheetFieldRequest request, int index, Guid userId)
    {
        var field = new WorksheetField
        {
            Id = Guid.NewGuid(),
            Order = request.Order == 0 ? index + 1 : request.Order,
            FieldKey = request.FieldKey?.Trim(),
            Label = request.Label?.Trim(),
            Type = request.Type,
            Mode = request.Mode,
            Unit = request.Unit,
            Analyte = request.Analyte,
            ConstantValue = request.ConstantValue,
            FormulaExpression = request.FormulaExpression,
            ColumnDefinitions = request.ColumnDefinitions,
            OptionsJson = WorksheetFieldOptions.Normalize(request.OptionsJson),
            ReferencedResultSourceTemplateId = request.ReferencedResultSourceTemplateId,
            ReferencedResultSourceFieldKey = request.ReferencedResultSourceFieldKey,
            ReferencedResultResolutionFieldKey = request.ReferencedResultResolutionFieldKey,
            CreatedAt = DateTime.UtcNow,
            CreatedById = userId
        };

        field.Revisions = [SnapshotOf(field, 1, userId)];
        return field;
    }

    private static WorksheetField CloneField(WorksheetField source, Guid userId)
    {
        var field = new WorksheetField
        {
            Id = Guid.NewGuid(),
            Order = source.Order,
            FieldKey = source.FieldKey,
            Label = source.Label,
            Type = source.Type,
            Mode = source.Mode,
            Unit = source.Unit,
            Analyte = source.Analyte,
            ConstantValue = source.ConstantValue,
            FormulaExpression = source.FormulaExpression,
            ColumnDefinitions = source.ColumnDefinitions,
            OptionsJson = source.OptionsJson,
            ReferencedResultSourceTemplateId = source.ReferencedResultSourceTemplateId,
            ReferencedResultSourceFieldKey = source.ReferencedResultSourceFieldKey,
            ReferencedResultResolutionFieldKey = source.ReferencedResultResolutionFieldKey,
            CreatedAt = DateTime.UtcNow,
            CreatedById = userId
        };

        // A new version starts its own revision history; the previous version keeps its own.
        field.Revisions = [SnapshotOf(field, 1, userId)];
        return field;
    }

    /// <summary>
    /// Matches incoming fields to existing ones by FieldKey — the stable business key, which
    /// is unique template-wide — so a field keeps its identity and its revision history
    /// across edits, and may even move between sections.
    /// </summary>
    private void MergeSections(
        WorksheetTemplate template, List<CreateWorksheetSectionRequest> requested, Guid userId)
    {
        var incoming = requested ?? [];
        var originalSections = template.Sections.ToList();
        var originalFields = originalSections
            .SelectMany(section => section.Fields)
            .ToList();
        // Older concurrent saves could leave duplicate keys in a draft. Keep the oldest
        // field (and its revision history), then remove the other rows during the merge.
        var existingFields = originalFields
            .GroupBy(field => field.FieldKey ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key,
                group => group.OrderBy(field => field.CreatedAt).ThenBy(field => field.Id).First(),
                StringComparer.OrdinalIgnoreCase);

        var keptFieldIds = new HashSet<Guid>();
        var keptSectionIds = new HashSet<Guid>();
        var resultSections = new List<WorksheetSection>();

        foreach (var (sectionRequest, sectionIndex) in incoming
                     .OrderBy(section => section.Order)
                     .Select((section, index) => (section, index)))
        {
            var section = template.Sections.FirstOrDefault(existing =>
                string.Equals(existing.Name, sectionRequest.Name?.Trim(), StringComparison.OrdinalIgnoreCase));

            if (section is null)
            {
                section = new WorksheetSection
                {
                    Id = Guid.NewGuid(),
                    Name = sectionRequest.Name?.Trim(),
                    CreatedAt = DateTime.UtcNow,
                    CreatedById = userId,
                    WorksheetTemplateId = template.Id,
                    Fields = []
                };
                context.QcWorksheetSections.Add(section);
            }

            section.Order = sectionRequest.Order == 0 ? sectionIndex + 1 : sectionRequest.Order;
            section.InstrumentId = sectionRequest.InstrumentId;
            section.UpdatedAt = DateTime.UtcNow;
            section.LastUpdatedById = userId;
            keptSectionIds.Add(section.Id);

            var fields = new List<WorksheetField>();

            foreach (var (fieldRequest, fieldIndex) in (sectionRequest.Fields ?? [])
                         .OrderBy(field => field.Order)
                         .Select((field, index) => (field, index)))
            {
                var key = fieldRequest.FieldKey?.Trim() ?? string.Empty;

                if (existingFields.TryGetValue(key, out var field))
                {
                    var changed = HasConfigurationChanged(field, fieldRequest);

                    field.Order = fieldRequest.Order == 0 ? fieldIndex + 1 : fieldRequest.Order;
                    field.Label = fieldRequest.Label?.Trim();
                    field.Type = fieldRequest.Type;
                    field.Mode = fieldRequest.Mode;
                    field.Unit = fieldRequest.Unit;
                    field.Analyte = fieldRequest.Analyte;
                    field.ConstantValue = fieldRequest.ConstantValue;
                    field.FormulaExpression = fieldRequest.FormulaExpression;
                    field.ColumnDefinitions = fieldRequest.ColumnDefinitions;
                    field.OptionsJson = WorksheetFieldOptions.Normalize(fieldRequest.OptionsJson);
                    field.ReferencedResultSourceTemplateId = fieldRequest.ReferencedResultSourceTemplateId;
                    field.ReferencedResultSourceFieldKey = fieldRequest.ReferencedResultSourceFieldKey;
                    field.ReferencedResultResolutionFieldKey = fieldRequest.ReferencedResultResolutionFieldKey;
                    field.WorksheetSectionId = section.Id;
                    field.UpdatedAt = DateTime.UtcNow;
                    field.LastUpdatedById = userId;

                    // Snapshot only on an actual configuration change, so repeated saves of
                    // an unchanged field do not inflate its history.
                    if (changed)
                    {
                        var next = (field.Revisions?.Count ?? 0) + 1;
                        var snapshot = SnapshotOf(field, next, userId);
                        snapshot.WorksheetFieldId = field.Id;
                        context.QcWorksheetFieldRevisions.Add(snapshot);
                    }

                    keptFieldIds.Add(field.Id);
                    fields.Add(field);
                }
                else
                {
                    var created = NewField(fieldRequest, fieldIndex, userId);
                    created.WorksheetSectionId = section.Id;
                    context.QcWorksheetFields.Add(created);
                    existingFields[key] = created;
                    keptFieldIds.Add(created.Id);
                    fields.Add(created);
                }
            }

            section.Fields = fields;
            resultSections.Add(section);
        }

        var removedFields = originalFields
            .Where(field => !keptFieldIds.Contains(field.Id))
            .ToList();

        if (removedFields.Count > 0)
            context.QcWorksheetFields.RemoveRange(removedFields);

        var removedSections = originalSections
            .Where(section => !keptSectionIds.Contains(section.Id))
            .ToList();

        if (removedSections.Count > 0)
            context.QcWorksheetSections.RemoveRange(removedSections);

        template.Sections = resultSections;
    }

    private static bool HasConfigurationChanged(WorksheetField field, CreateWorksheetFieldRequest request) =>
        field.Label != request.Label?.Trim()
        || field.Type != request.Type
        || field.Mode != request.Mode
        || field.Unit != request.Unit
        || field.Analyte != request.Analyte
        || field.ConstantValue != request.ConstantValue
        || field.FormulaExpression != request.FormulaExpression
        || field.ColumnDefinitions != request.ColumnDefinitions
        || field.OptionsJson != WorksheetFieldOptions.Normalize(request.OptionsJson)
        || field.ReferencedResultSourceTemplateId != request.ReferencedResultSourceTemplateId
        || field.ReferencedResultSourceFieldKey != request.ReferencedResultSourceFieldKey
        || field.ReferencedResultResolutionFieldKey != request.ReferencedResultResolutionFieldKey;

    private static WorksheetFieldRevision SnapshotOf(WorksheetField field, int revisionNumber, Guid userId) => new()
    {
        Id = Guid.NewGuid(),
        WorksheetFieldId = field.Id,
        RevisionNumber = revisionNumber,
        FieldKey = field.FieldKey,
        Label = field.Label,
        Type = field.Type,
        Mode = field.Mode,
        Unit = field.Unit,
        Analyte = field.Analyte,
        ConstantValue = field.ConstantValue,
        FormulaExpression = field.FormulaExpression,
        ColumnDefinitions = field.ColumnDefinitions,
        OptionsJson = field.OptionsJson,
        ReferencedResultSourceTemplateId = field.ReferencedResultSourceTemplateId,
        ReferencedResultSourceFieldKey = field.ReferencedResultSourceFieldKey,
        ReferencedResultResolutionFieldKey = field.ReferencedResultResolutionFieldKey,
        Order = field.Order,
        CreatedAt = DateTime.UtcNow,
        CreatedById = userId
    };

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private async Task<bool> HasConfiguredApprovalChain()
    {
        var approval = await context.Approvals.FirstOrDefaultAsync(item => item.ItemType == ModelType);
        if (approval is null)
            return false;

        return await context.ApprovalStages.AnyAsync(stage => stage.ApprovalId == approval.Id);
    }

    private async Task<WorksheetTemplate> LoadDetail(Guid id) =>
        await context.QcWorksheetTemplates
            .Include(item => item.CreatedBy)
            .Include(item => item.Stp)
            .Include(item => item.Sections.OrderBy(section => section.Order))
                .ThenInclude(section => section.Fields.OrderBy(field => field.Order))
                    .ThenInclude(field => field.Revisions.OrderBy(revision => revision.RevisionNumber))
            .AsSplitQuery()
            .SingleOrDefaultAsync(item => item.Id == id);

    private WorksheetTemplateSummaryDto ToSummaryDto(WorksheetTemplate template) => new()
    {
        Id = template.Id,
        Code = template.Code,
        Name = template.Name,
        Department = template.Department,
        Category = template.Category,
        Version = template.Version,
        Status = template.Status,
        Approved = template.Approved,
        EffectiveDate = template.EffectiveDate,
        SupersedesId = template.SupersedesId,
        StpId = template.StpId,
        CreatedAt = template.CreatedAt,
        CreatedBy = mapper.Map<UserDto>(template.CreatedBy)
    };

    private WorksheetTemplateDetailDto ToDetailDto(WorksheetTemplate template) => new()
    {
        Id = template.Id,
        Code = template.Code,
        Name = template.Name,
        Department = template.Department,
        Category = template.Category,
        Version = template.Version,
        Status = template.Status,
        Approved = template.Approved,
        EffectiveDate = template.EffectiveDate,
        SupersedesId = template.SupersedesId,
        StpId = template.StpId,
        CreatedAt = template.CreatedAt,
        CreatedBy = mapper.Map<UserDto>(template.CreatedBy),
        Stp = template.Stp is null
            ? null
            : new StpReferenceDto
            {
                Id = template.Stp.Id,
                Code = template.Stp.Code,
                Name = template.Stp.Name,
                Version = template.Stp.Version,
                Status = template.Stp.Status
            },
        Sections = template.Sections
            .OrderBy(section => section.Order)
            .Select(section => new WorksheetSectionDto
            {
                Id = section.Id,
                Order = section.Order,
                Name = section.Name,
                InstrumentId = section.InstrumentId,
                CreatedAt = section.CreatedAt,
                Fields = section.Fields
                    .OrderBy(field => field.Order)
                    .Select(field => new WorksheetFieldDto
                    {
                        Id = field.Id,
                        Order = field.Order,
                        FieldKey = field.FieldKey,
                        Label = field.Label,
                        Type = field.Type,
                        Mode = field.Mode,
                        Unit = field.Unit,
                        Analyte = field.Analyte,
                        ConstantValue = field.ConstantValue,
                        FormulaExpression = field.FormulaExpression,
                        ColumnDefinitions = field.ColumnDefinitions,
                        OptionsJson = field.OptionsJson,
                        ReferencedResultSourceTemplateId = field.ReferencedResultSourceTemplateId,
                        ReferencedResultSourceFieldKey = field.ReferencedResultSourceFieldKey,
                        ReferencedResultResolutionFieldKey = field.ReferencedResultResolutionFieldKey,
                        CreatedAt = field.CreatedAt,
                        Revisions = (field.Revisions ?? [])
                            .OrderBy(revision => revision.RevisionNumber)
                            .Select(revision => new WorksheetFieldRevisionDto
                            {
                                Id = revision.Id,
                                RevisionNumber = revision.RevisionNumber,
                                FieldKey = revision.FieldKey,
                                Label = revision.Label,
                                Type = revision.Type,
                                Mode = revision.Mode,
                                Unit = revision.Unit,
                                Analyte = revision.Analyte,
                                ConstantValue = revision.ConstantValue,
                                FormulaExpression = revision.FormulaExpression,
                                ColumnDefinitions = revision.ColumnDefinitions,
                                OptionsJson = revision.OptionsJson,
                                Order = revision.Order,
                                CreatedAt = revision.CreatedAt
                            })
                            .ToList()
                    })
                    .ToList()
            })
            .ToList()
    };
}
