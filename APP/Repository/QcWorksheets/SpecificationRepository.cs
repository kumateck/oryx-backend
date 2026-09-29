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

/// <summary>
/// The Specification document: the acceptance criteria a TestRequest's results are judged
/// against. Follows Milestone 1's lifecycle shape exactly — the same
/// Draft -> UnderReview -> Approved -> Effective -> Superseded transitions, the same
/// edit-triggers-versioning enforcement, and the same centralized QcApproval/re-auth
/// signature path — so the shared governance rules cannot drift per entity.
/// <para>
/// Additive: nothing here reads or writes the live <c>MaterialSpecification</c>/
/// <c>ProductSpecification</c> tables.
/// </para>
/// </summary>
public class SpecificationRepository(
    ApplicationDbContext context,
    IMapper mapper,
    IQcSignatureService signatureService,
    IQcReauthContext reauthContext,
    IApprovalRepository approvalRepository) : ISpecificationRepository
{
    private const string ModelType = QcWorksheetModelTypes.Specification;

    // -----------------------------------------------------------------------
    // Reads
    // -----------------------------------------------------------------------

    public async Task<Result<Paginateable<IEnumerable<SpecificationSummaryDto>>>> GetSpecifications(
        int page, int pageSize, string searchQuery, QcDocumentStatus? status,
        SpecificationAppliesTo? appliesTo)
    {
        var query = context.QcSpecifications
            .Include(item => item.CreatedBy)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            var term = searchQuery.Trim().ToLower();
            query = query.Where(item =>
                item.Code.ToLower().Contains(term)
                || item.Name.ToLower().Contains(term));
        }

        if (status.HasValue)
            query = query.Where(item => item.Status == status.Value);

        if (appliesTo.HasValue)
            query = query.Where(item => item.AppliesTo == appliesTo.Value);

        return await PaginationHelper.GetPaginatedResultAsync(
            query.OrderByDescending(item => item.CreatedAt),
            page,
            pageSize,
            ToSummaryDto);
    }

    public async Task<Result<SpecificationDetailDto>> GetSpecification(Guid id)
    {
        var specification = await LoadDetail(id);
        return specification is null
            ? Result.Failure<SpecificationDetailDto>(QcWorksheetErrors.SpecificationNotFound(id))
            : Result.Success(ToDetailDto(specification));
    }

    /// <summary>
    /// Powers the SourceFieldKey dropdown. Every field on each link's <b>pinned</b> template
    /// version is returned, not just the Result/CalculatedValue ones: those are the realistic
    /// candidates, but filtering stays on the client so a future field type that warrants COA
    /// inclusion needs no backend change.
    /// <para>
    /// The pinned version is what the dropdown offers, so an author can only ever pick a
    /// field that the same version's validation will accept. If a newer template version has
    /// since become Effective, its fields deliberately do not appear here — adopting it means
    /// re-pointing the link, which is an edit of the Specification.
    /// </para>
    /// </summary>
    public async Task<Result<List<SpecificationAvailableFieldDto>>> GetAvailableFields(Guid id)
    {
        var specification = await context.QcSpecifications
            .AsNoTracking()
            .Include(item => item.WorksheetLinks)
            .SingleOrDefaultAsync(item => item.Id == id);

        if (specification is null)
            return Result.Failure<List<SpecificationAvailableFieldDto>>(
                QcWorksheetErrors.SpecificationNotFound(id));

        var fields = new List<SpecificationAvailableFieldDto>();

        foreach (var link in specification.WorksheetLinks)
        {
            var template = await LoadPinnedTemplate(link.WorksheetTemplateId);
            if (template is null)
                continue;

            fields.AddRange(
                from section in template.Sections.OrderBy(section => section.Order)
                from field in section.Fields.OrderBy(field => field.Order)
                select new SpecificationAvailableFieldDto
                {
                    WorksheetTemplateId = template.Id,
                    WorksheetTemplateCode = template.Code,
                    WorksheetTemplateName = template.Name,

                    // The link's own pin, not the row's current version — they agree, since a
                    // template version row is immutable, and reporting the pin makes the
                    // contract explicit.
                    WorksheetTemplateVersion = link.WorksheetTemplateVersion,
                    AnalysisType = link.AnalysisType,
                    SectionName = section.Name,
                    FieldKey = field.FieldKey,
                    Label = field.Label,
                    Type = field.Type,
                    Mode = field.Mode,
                    Unit = field.Unit,
                    Analyte = field.Analyte,
                    ConstantValue = field.ConstantValue
                });
        }

        return Result.Success(fields);
    }

    // -----------------------------------------------------------------------
    // Authoring
    // -----------------------------------------------------------------------

    public async Task<Result<SpecificationDetailDto>> CreateSpecification(
        CreateSpecificationRequest request, Guid userId)
    {
        var validation = await Validate(request);
        if (!validation.IsSuccess)
            return Result.Failure<SpecificationDetailDto>(validation.Error);

        var specificationId = Guid.NewGuid();
        var pinnedVersions = await LoadPinnedVersions(request.WorksheetLinks);

        var specification = new Specification
        {
            Id = specificationId,
            Code = request.Code?.Trim(),
            Name = request.Name?.Trim(),
            AppliesTo = request.AppliesTo!.Value,
            Stage = request.Stage,
            RetestPolicy = request.RetestPolicy!.Value,
            Version = 1,
            Status = QcDocumentStatus.Draft,
            Approved = false,
            CreatedAt = DateTime.UtcNow,
            CreatedById = userId,
            WorksheetLinks = BuildLinks(request.WorksheetLinks, userId, pinnedVersions),
            Characteristics = BuildCharacteristics(request.Characteristics, userId)
        };

        context.QcSpecifications.Add(specification);
        await context.SaveChangesAsync();

        return await GetSpecification(specification.Id);
    }

    public async Task<Result<SpecificationDetailDto>> UpdateSpecification(
        Guid id, UpdateSpecificationRequest request, Guid userId)
    {
        var specification = await context.QcSpecifications.SingleOrDefaultAsync(item => item.Id == id);
        if (specification is null)
            return Result.Failure<SpecificationDetailDto>(QcWorksheetErrors.SpecificationNotFound(id));

        // Edit-triggers-versioning, enforced server-side. Client-side read-only rendering is
        // never trusted on its own.
        if (specification.Status is not (QcDocumentStatus.Draft or QcDocumentStatus.UnderReview))
            return Result.Failure<SpecificationDetailDto>(
                QcWorksheetErrors.NotEditableInStatus(specification.Status));

        var validation = await Validate(request);
        if (!validation.IsSuccess)
            return Result.Failure<SpecificationDetailDto>(validation.Error);

        specification.Code = request.Code?.Trim();
        specification.Name = request.Name?.Trim();
        specification.AppliesTo = request.AppliesTo!.Value;
        specification.Stage = request.Stage;
        specification.RetestPolicy = request.RetestPolicy!.Value;
        specification.UpdatedAt = DateTime.UtcNow;
        specification.LastUpdatedById = userId;

        // A reviewer must not be evaluating a moving target: any edit while under review
        // sends the document back to Draft, so review starts over.
        if (specification.Status == QcDocumentStatus.UnderReview)
        {
            specification.Status = QcDocumentStatus.Draft;
            specification.Approved = false;
        }

        // Children are replaced through their own DbSets rather than through the navigation
        // properties: the context soft-deletes by rewriting a Deleted entry as Modified,
        // which does not survive EF's orphan-cascade bookkeeping.
        var existingLinks = await context.QcSpecificationWorksheetLinks
            .Where(link => link.SpecificationId == id)
            .ToListAsync();
        context.QcSpecificationWorksheetLinks.RemoveRange(existingLinks);

        var existingCharacteristics = await context.QcSpecificationCharacteristics
            .Where(characteristic => characteristic.SpecificationId == id)
            .ToListAsync();
        context.QcSpecificationCharacteristics.RemoveRange(existingCharacteristics);

        // Re-pinned from whichever template rows the request now points at. Keeping the same
        // template id re-reads the same immutable version row, so an unrelated edit never
        // moves the pin; picking a different version's id in the dropdown is what moves it.
        var pinnedVersions = await LoadPinnedVersions(request.WorksheetLinks);

        foreach (var link in BuildLinks(request.WorksheetLinks, userId, pinnedVersions))
        {
            link.SpecificationId = id;
            context.QcSpecificationWorksheetLinks.Add(link);
        }

        foreach (var characteristic in BuildCharacteristics(request.Characteristics, userId))
        {
            characteristic.SpecificationId = id;
            context.QcSpecificationCharacteristics.Add(characteristic);
        }

        await context.SaveChangesAsync();
        return await GetSpecification(id);
    }

    public async Task<Result<SpecificationDetailDto>> CreateNewVersion(Guid id, Guid userId)
    {
        var source = await LoadDetail(id);
        if (source is null)
            return Result.Failure<SpecificationDetailDto>(QcWorksheetErrors.SpecificationNotFound(id));

        if (source.Status != QcDocumentStatus.Effective)
            return Result.Failure<SpecificationDetailDto>(
                QcWorksheetErrors.NewVersionRequiresEffective(source.Status));

        // The source record is untouched and stays in force until the new draft completes its
        // own full approval cycle.
        var draft = new Specification
        {
            Id = Guid.NewGuid(),
            Code = source.Code,
            Name = source.Name,
            AppliesTo = source.AppliesTo,
            Stage = source.Stage,
            RetestPolicy = source.RetestPolicy,
            Version = source.Version + 1,
            SupersedesId = source.Id,
            Status = QcDocumentStatus.Draft,
            Approved = false,
            CreatedAt = DateTime.UtcNow,
            CreatedById = userId,
            WorksheetLinks = source.WorksheetLinks
                .Select(link => new SpecificationWorksheetLink
                {
                    Id = Guid.NewGuid(),
                    WorksheetTemplateId = link.WorksheetTemplateId,

                    // The clone inherits the predecessor's pin rather than silently adopting
                    // whatever is Effective now. Creating a new Specification version is not
                    // by itself a decision to change worksheet template version; the author
                    // re-points the link explicitly if that is what they mean.
                    WorksheetTemplateVersion = link.WorksheetTemplateVersion,
                    AnalysisType = link.AnalysisType,
                    CreatedAt = DateTime.UtcNow,
                    CreatedById = userId
                })
                .ToList(),
            Characteristics = source.Characteristics
                .OrderBy(characteristic => characteristic.DisplayOrder)
                .Select(characteristic => new SpecificationCharacteristic
                {
                    Id = Guid.NewGuid(),
                    TestName = characteristic.TestName,
                    Analyte = characteristic.Analyte,
                    AcceptanceCriteria = characteristic.AcceptanceCriteria,
                    AlertLimit = characteristic.AlertLimit,
                    ActionLimit = characteristic.ActionLimit,
                    SamplingPointGroupId = characteristic.SamplingPointGroupId,
                    SourceWorksheetTemplateId = characteristic.SourceWorksheetTemplateId,
                    SourceFieldKey = characteristic.SourceFieldKey,
                    IncludeOnCoa = characteristic.IncludeOnCoa,
                    DisplayOrder = characteristic.DisplayOrder,
                    GroupName = characteristic.GroupName,
                    CreatedAt = DateTime.UtcNow,
                    CreatedById = userId
                })
                .ToList()
        };

        context.QcSpecifications.Add(draft);
        await context.SaveChangesAsync();

        return await GetSpecification(draft.Id);
    }

    // -----------------------------------------------------------------------
    // Lifecycle
    // -----------------------------------------------------------------------

    public async Task<Result<SpecificationDetailDto>> SubmitForReview(Guid id, Guid userId)
    {
        var specification = await context.QcSpecifications.SingleOrDefaultAsync(item => item.Id == id);
        if (specification is null)
            return Result.Failure<SpecificationDetailDto>(QcWorksheetErrors.SpecificationNotFound(id));

        if (specification.Status != QcDocumentStatus.Draft)
            return Result.Failure<SpecificationDetailDto>(
                QcWorksheetErrors.SubmitRequiresDraft(specification.Status));

        // QC opts out of the engine's silent auto-approval fallback: without a configured
        // chain there is nobody to sign, so this fails rather than self-approving.
        if (!await HasConfiguredApprovalChain())
            return Result.Failure<SpecificationDetailDto>(
                QcWorksheetErrors.NoApprovalWorkflowConfigured(ModelType));

        specification.Status = QcDocumentStatus.UnderReview;
        specification.UpdatedAt = DateTime.UtcNow;
        specification.LastUpdatedById = userId;
        await context.SaveChangesAsync();

        await approvalRepository.CreateInitialApprovalsAsync(ModelType, id);

        return await GetSpecification(id);
    }

    public async Task<Result<SpecificationDetailDto>> Approve(
        Guid id, QcApprovalRequest request, Guid userId, List<Guid> roleIds)
    {
        var specification = await context.QcSpecifications.SingleOrDefaultAsync(item => item.Id == id);
        if (specification is null)
            return Result.Failure<SpecificationDetailDto>(QcWorksheetErrors.SpecificationNotFound(id));

        if (specification.Status != QcDocumentStatus.UnderReview)
            return Result.Failure<SpecificationDetailDto>(
                QcWorksheetErrors.ApproveRequiresUnderReview(specification.Status));

        var signed = await signatureService.SignAndApproveAsync(
            ModelType, id, userId, roleIds, request.Password, request.Comments);

        return !signed.IsSuccess
            ? Result.Failure<SpecificationDetailDto>(signed.Error)
            : await GetSpecification(id);
    }

    public async Task<Result<SpecificationDetailDto>> Reject(
        Guid id, QcApprovalRequest request, Guid userId, List<Guid> roleIds)
    {
        var specification = await context.QcSpecifications.SingleOrDefaultAsync(item => item.Id == id);
        if (specification is null)
            return Result.Failure<SpecificationDetailDto>(QcWorksheetErrors.SpecificationNotFound(id));

        if (specification.Status != QcDocumentStatus.UnderReview)
            return Result.Failure<SpecificationDetailDto>(
                QcWorksheetErrors.ApproveRequiresUnderReview(specification.Status));

        var signed = await signatureService.SignAndRejectAsync(
            ModelType, id, userId, roleIds, request.Password, request.Comments);

        return !signed.IsSuccess
            ? Result.Failure<SpecificationDetailDto>(signed.Error)
            : await GetSpecification(id);
    }

    public async Task<Result<SpecificationDetailDto>> MakeEffective(Guid id, Guid userId)
    {
        var specification = await context.QcSpecifications.SingleOrDefaultAsync(item => item.Id == id);
        if (specification is null)
            return Result.Failure<SpecificationDetailDto>(QcWorksheetErrors.SpecificationNotFound(id));

        if (specification.Status != QcDocumentStatus.Approved)
            return Result.Failure<SpecificationDetailDto>(
                QcWorksheetErrors.MakeEffectiveRequiresApproved(specification.Status));

        specification.Status = QcDocumentStatus.Effective;
        specification.EffectiveDate = DateTime.UtcNow;
        specification.UpdatedAt = DateTime.UtcNow;
        specification.LastUpdatedById = userId;

        // The predecessor only retires now — not when this version was drafted.
        if (specification.SupersedesId.HasValue)
        {
            var superseded = await context.QcSpecifications
                .SingleOrDefaultAsync(item => item.Id == specification.SupersedesId.Value);

            if (superseded is { Status: QcDocumentStatus.Effective })
            {
                superseded.Status = QcDocumentStatus.Superseded;
                superseded.UpdatedAt = DateTime.UtcNow;
                superseded.LastUpdatedById = userId;
            }
        }

        await context.SaveChangesAsync();
        return await GetSpecification(id);
    }

    public async Task<Result<SpecificationDetailDto>> Supersede(
        Guid id, QcSupersedeRequest request, Guid userId)
    {
        var specification = await context.QcSpecifications.SingleOrDefaultAsync(item => item.Id == id);
        if (specification is null)
            return Result.Failure<SpecificationDetailDto>(QcWorksheetErrors.SpecificationNotFound(id));

        if (specification.Status != QcDocumentStatus.Effective)
            return Result.Failure<SpecificationDetailDto>(
                QcWorksheetErrors.SupersedeRequiresEffective(specification.Status));

        if (string.IsNullOrWhiteSpace(request.Comments))
            return Result.Failure<SpecificationDetailDto>(QcWorksheetErrors.ReasonForChangeRequired);

        var approval = await context.Approvals.FirstOrDefaultAsync(item => item.ItemType == ModelType);
        if (approval is null)
            return Result.Failure<SpecificationDetailDto>(
                QcWorksheetErrors.NoApprovalWorkflowConfigured(ModelType));

        var verified = await signatureService.VerifyAsync(userId, request.Password);
        if (!verified.IsSuccess)
            return Result.Failure<SpecificationDetailDto>(verified.Error);

        var recorded = await QcApprovalHandler.RecordSignedActionAsync(
            context,
            reauthContext,
            ModelType,
            id,
            approval.Id,
            userId,
            request.Comments);

        if (!recorded.IsSuccess)
            return Result.Failure<SpecificationDetailDto>(recorded.Error);

        specification.Status = QcDocumentStatus.Superseded;
        specification.UpdatedAt = DateTime.UtcNow;
        specification.LastUpdatedById = userId;
        await context.SaveChangesAsync();

        return await GetSpecification(id);
    }

    // -----------------------------------------------------------------------
    // Validation
    // -----------------------------------------------------------------------

    private async Task<Result> Validate(CreateSpecificationRequest request)
    {
        if (!request.AppliesTo.HasValue)
            return QcWorksheetErrors.AppliesToRequired;

        // No default, and no fall-back: an unset policy is rejected rather than read as
        // SameSample. The enum's zero value is deliberately unassigned so this also catches a
        // payload that sent a raw 0.
        if (!request.RetestPolicy.HasValue || !Enum.IsDefined(request.RetestPolicy.Value))
            return QcWorksheetErrors.RetestPolicyRequired;

        var appliesTo = request.AppliesTo.Value;

        // Stage is Product-only, not merely optional elsewhere: a Stage on a RawMaterial
        // specification would be meaningless data that a later COA or TestRequest could read.
        if (appliesTo == SpecificationAppliesTo.Product)
        {
            if (!request.Stage.HasValue)
                return QcWorksheetErrors.StageRequiredForProduct;
        }
        else if (request.Stage.HasValue)
        {
            return QcWorksheetErrors.StageIsProductOnly(appliesTo);
        }

        var links = request.WorksheetLinks ?? [];

        var seen = new HashSet<SpecificationAnalysisType>();
        foreach (var link in links)
        {
            if (!link.AnalysisType.HasValue)
                return Error.Validation(
                    "QcSpecification.AnalysisTypeRequired",
                    "Each worksheet link must declare whether it is the Chemical or Microbial track.");

            var analysisType = link.AnalysisType.Value;

            // At most one Chemical and one Microbial link. An application-layer rule by
            // design — see SpecificationWorksheetLinkConfiguration for why it is not an index.
            if (!seen.Add(analysisType))
                return QcWorksheetErrors.DuplicateAnalysisTypeLink(analysisType);

            if (appliesTo == SpecificationAppliesTo.RoutineEnvironmental
                && analysisType != SpecificationAnalysisType.Microbial)
                return QcWorksheetErrors.EnvironmentalIsMicrobialOnly;

            if (!await context.QcWorksheetTemplates.AnyAsync(item => item.Id == link.WorksheetTemplateId))
                return QcWorksheetErrors.LinkedTemplateNotFound(link.WorksheetTemplateId);
        }

        var linkedTemplateIds = links.Select(link => link.WorksheetTemplateId).ToHashSet();

        // The source template's field keys are resolved once per template rather than per
        // characteristic, since one EM test legitimately produces many rows off one template.
        var fieldKeysByTemplate = new Dictionary<Guid, (WorksheetTemplate Template, HashSet<string> Keys)>();

        foreach (var characteristic in request.Characteristics ?? [])
        {
            // Referential integrity alone would accept any template in the system; the rule is
            // that a characteristic may only source a field from a template THIS
            // specification links.
            if (!linkedTemplateIds.Contains(characteristic.SourceWorksheetTemplateId))
                return QcWorksheetErrors.CharacteristicTemplateNotLinked(
                    characteristic.SourceWorksheetTemplateId);

            if (!fieldKeysByTemplate.TryGetValue(characteristic.SourceWorksheetTemplateId, out var resolved))
            {
                // The pinned version, never a newer Effective successor: a Specification's
                // field validation must not shift when its linked template is superseded.
                var template = await LoadPinnedTemplate(characteristic.SourceWorksheetTemplateId);
                if (template is null)
                    return QcWorksheetErrors.LinkedTemplateNotFound(characteristic.SourceWorksheetTemplateId);

                resolved = (
                    template,
                    template.Sections
                        .SelectMany(section => section.Fields)
                        .Select(field => field.FieldKey)
                        .Where(key => !string.IsNullOrWhiteSpace(key))
                        .ToHashSet(StringComparer.OrdinalIgnoreCase));

                fieldKeysByTemplate[characteristic.SourceWorksheetTemplateId] = resolved;
            }

            if (!resolved.Keys.Contains(characteristic.SourceFieldKey?.Trim() ?? string.Empty))
                return QcWorksheetErrors.CharacteristicFieldKeyNotFound(
                    characteristic.SourceFieldKey, resolved.Template.Code, resolved.Template.Version);

            // Dropdown-only: there is no free-text path to this field on the request, and an
            // id that resolves to nothing is the same failure a typo would have been.
            if (characteristic.SamplingPointGroupId.HasValue
                && !await context.QcSamplingPointGroups.AnyAsync(
                    item => item.Id == characteristic.SamplingPointGroupId.Value))
                return QcWorksheetErrors.SamplingPointGroupNotFoundOnCharacteristic(
                    characteristic.SamplingPointGroupId.Value);
        }

        return Result.Success();
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    /// <summary>
    /// Loads the exact worksheet template version a link is pinned to — the row the link's
    /// <c>WorksheetTemplateId</c> names, and nothing else.
    /// <para>
    /// Hard version pinning, matching the locked governance rule already applied to
    /// <c>TestRequest</c>/<c>WorksheetInstance</c> (lifecycle-and-governance.md, "Version
    /// pinning"). There is deliberately no walk forward through <c>SupersedesId</c>: when the
    /// pinned version is superseded, this Specification keeps resolving against the pinned
    /// version, so neither its stored characteristics nor the fields offered for new ones
    /// shift underneath it. Adopting a newer template version is an explicit edit of the
    /// Specification, which — once it is Effective — requires a new Specification version
    /// under edit-triggers-versioning.
    /// </para>
    /// </summary>
    private async Task<WorksheetTemplate> LoadPinnedTemplate(Guid templateId) =>
        await LoadTemplateWithFields(templateId);

    private async Task<WorksheetTemplate> LoadTemplateWithFields(Guid templateId) =>
        await context.QcWorksheetTemplates
            .AsNoTracking()
            .Include(item => item.Sections.OrderBy(section => section.Order))
                .ThenInclude(section => section.Fields.OrderBy(field => field.Order))
            .AsSplitQuery()
            .SingleOrDefaultAsync(item => item.Id == templateId);

    private async Task<bool> HasConfiguredApprovalChain()
    {
        var approval = await context.Approvals.FirstOrDefaultAsync(item => item.ItemType == ModelType);
        if (approval is null)
            return false;

        return await context.ApprovalStages.AnyAsync(stage => stage.ApprovalId == approval.Id);
    }

    /// <summary>
    /// Reads the version of each linked template straight from the template rows. The pin is
    /// captured server-side and is never client-supplied — a request carrying its own version
    /// number could otherwise claim a pin that does not match the row it points at.
    /// </summary>
    private async Task<Dictionary<Guid, int>> LoadPinnedVersions(
        List<CreateSpecificationWorksheetLinkRequest> links)
    {
        var templateIds = (links ?? [])
            .Select(link => link.WorksheetTemplateId)
            .Distinct()
            .ToList();

        if (templateIds.Count == 0)
            return [];

        return await context.QcWorksheetTemplates
            .AsNoTracking()
            .Where(template => templateIds.Contains(template.Id))
            .ToDictionaryAsync(template => template.Id, template => template.Version);
    }

    private static List<SpecificationWorksheetLink> BuildLinks(
        List<CreateSpecificationWorksheetLinkRequest> links,
        Guid userId,
        IReadOnlyDictionary<Guid, int> pinnedVersions) =>
        (links ?? [])
        .Select(link => new SpecificationWorksheetLink
        {
            Id = Guid.NewGuid(),
            WorksheetTemplateId = link.WorksheetTemplateId,

            // Pinned at save time. Validation already proved the template exists, so the
            // lookup cannot miss; the fallback only keeps this total.
            WorksheetTemplateVersion = pinnedVersions.TryGetValue(link.WorksheetTemplateId, out var version)
                ? version
                : 0,
            AnalysisType = link.AnalysisType!.Value,
            CreatedAt = DateTime.UtcNow,
            CreatedById = userId
        })
        .ToList();

    private static List<SpecificationCharacteristic> BuildCharacteristics(
        List<CreateSpecificationCharacteristicRequest> characteristics, Guid userId) =>
        (characteristics ?? [])
        .OrderBy(characteristic => characteristic.DisplayOrder)
        .Select((characteristic, index) => new SpecificationCharacteristic
        {
            Id = Guid.NewGuid(),
            TestName = characteristic.TestName?.Trim(),
            Analyte = characteristic.Analyte?.Trim(),
            AcceptanceCriteria = characteristic.AcceptanceCriteria?.Trim(),
            AlertLimit = characteristic.AlertLimit?.Trim(),
            ActionLimit = characteristic.ActionLimit?.Trim(),
            SamplingPointGroupId = characteristic.SamplingPointGroupId,
            SourceWorksheetTemplateId = characteristic.SourceWorksheetTemplateId,
            SourceFieldKey = characteristic.SourceFieldKey?.Trim(),
            IncludeOnCoa = characteristic.IncludeOnCoa,
            DisplayOrder = characteristic.DisplayOrder == 0 ? index + 1 : characteristic.DisplayOrder,
            GroupName = characteristic.GroupName?.Trim(),
            CreatedAt = DateTime.UtcNow,
            CreatedById = userId
        })
        .ToList();

    /// <summary>
    /// Read path. No-tracking on purpose: the context soft-deletes by turning a Deleted entry
    /// into a Modified one, so previously removed children stay in the change tracker and
    /// would otherwise be fixed up back into this graph on a read-after-write.
    /// </summary>
    private async Task<Specification> LoadDetail(Guid id) =>
        await context.QcSpecifications
            .AsNoTracking()
            .Include(item => item.CreatedBy)
            .Include(item => item.WorksheetLinks)
                .ThenInclude(link => link.WorksheetTemplate)
            .Include(item => item.Characteristics.OrderBy(characteristic => characteristic.DisplayOrder))
                .ThenInclude(characteristic => characteristic.SamplingPointGroup)
            .AsSplitQuery()
            .SingleOrDefaultAsync(item => item.Id == id);

    private SpecificationSummaryDto ToSummaryDto(Specification specification) => new()
    {
        Id = specification.Id,
        Code = specification.Code,
        Name = specification.Name,
        AppliesTo = specification.AppliesTo,
        Stage = specification.Stage,
        Version = specification.Version,
        Status = specification.Status,
        Approved = specification.Approved,
        RetestPolicy = specification.RetestPolicy,
        EffectiveDate = specification.EffectiveDate,
        SupersedesId = specification.SupersedesId,
        CreatedAt = specification.CreatedAt,
        CreatedBy = mapper.Map<UserDto>(specification.CreatedBy)
    };

    private SpecificationDetailDto ToDetailDto(Specification specification) => new()
    {
        Id = specification.Id,
        Code = specification.Code,
        Name = specification.Name,
        AppliesTo = specification.AppliesTo,
        Stage = specification.Stage,
        Version = specification.Version,
        Status = specification.Status,
        Approved = specification.Approved,
        RetestPolicy = specification.RetestPolicy,
        EffectiveDate = specification.EffectiveDate,
        SupersedesId = specification.SupersedesId,
        CreatedAt = specification.CreatedAt,
        CreatedBy = mapper.Map<UserDto>(specification.CreatedBy),
        WorksheetLinks = specification.WorksheetLinks
            .OrderBy(link => link.AnalysisType)
            .Select(link => new SpecificationWorksheetLinkDto
            {
                Id = link.Id,
                WorksheetTemplateId = link.WorksheetTemplateId,
                WorksheetTemplateVersion = link.WorksheetTemplateVersion,
                AnalysisType = link.AnalysisType,
                CreatedAt = link.CreatedAt,
                WorksheetTemplate = link.WorksheetTemplate is null
                    ? null
                    : new WorksheetTemplateReferenceDto
                    {
                        Id = link.WorksheetTemplate.Id,
                        Code = link.WorksheetTemplate.Code,
                        Name = link.WorksheetTemplate.Name,
                        Version = link.WorksheetTemplate.Version,
                        Category = link.WorksheetTemplate.Category,
                        Status = link.WorksheetTemplate.Status
                    }
            })
            .ToList(),
        Characteristics = specification.Characteristics
            .OrderBy(characteristic => characteristic.DisplayOrder)
            .Select(characteristic => new SpecificationCharacteristicDto
            {
                Id = characteristic.Id,
                TestName = characteristic.TestName,
                Analyte = characteristic.Analyte,
                AcceptanceCriteria = characteristic.AcceptanceCriteria,
                AlertLimit = characteristic.AlertLimit,
                ActionLimit = characteristic.ActionLimit,
                SamplingPointGroupId = characteristic.SamplingPointGroupId,
                SamplingPointGroup = characteristic.SamplingPointGroup is null
                    ? null
                    : new SamplingPointGroupDto
                    {
                        Id = characteristic.SamplingPointGroup.Id,
                        Name = characteristic.SamplingPointGroup.Name,
                        Description = characteristic.SamplingPointGroup.Description,
                        CreatedAt = characteristic.SamplingPointGroup.CreatedAt
                    },
                SourceWorksheetTemplateId = characteristic.SourceWorksheetTemplateId,
                SourceFieldKey = characteristic.SourceFieldKey,
                IncludeOnCoa = characteristic.IncludeOnCoa,
                DisplayOrder = characteristic.DisplayOrder,
                GroupName = characteristic.GroupName,
                CreatedAt = characteristic.CreatedAt
            })
            .ToList()
    };
}
