using System.Data;
using System.Text.Json;
using APP.IRepository;
using AutoMapper;
using DOMAIN.Entities.QcWorksheets;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository.QcWorksheets;

/// <summary>
/// Build brief 08: the Specification proposals a worksheet import carried, kept on the server
/// and turned into a <b>Draft</b> Specification through a reviewed step.
/// <para>
/// Nothing here approves anything. Apply creates or updates the Specification only through
/// <see cref="ISpecificationRepository"/>'s ordinary create/update, so every M2 validation
/// rule runs unchanged and the normal lifecycle stays the only path to Effective.
/// </para>
/// </summary>
public class SpecificationProposalRepository(
    ApplicationDbContext context,
    IMapper mapper,
    ISpecificationRepository specifications) : ISpecificationProposalRepository
{
    /// <summary>The COA section heading every imported characteristic sits under.</summary>
    internal const string MicrobialGroupName = "MICROBIAL";

    internal const string EnvironmentalSpecificationName = "Environmental Monitoring";
    internal const string WaterSpecificationName = "Purified Water";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private static readonly ArdFamily[] SupportedFamilies =
    [
        ArdFamily.ProductMicro,
        ArdFamily.PurifiedWater,
        ArdFamily.EnvironmentalMonitoring
    ];

    // -----------------------------------------------------------------------
    // Store / read / dismiss
    // -----------------------------------------------------------------------

    public async Task<Result<SpecificationProposalSetDetailDto>> CreateProposalSet(
        CreateSpecificationProposalSetRequest request, Guid userId)
    {
        if (request.Family is not { } family || !SupportedFamilies.Contains(family))
            return Result.Failure<SpecificationProposalSetDetailDto>(
                QcWorksheetErrors.SpecificationProposalFamilyNotSupported(request.Family));

        // Media produce no Specification; a set with nothing to review is never stored.
        if (request.SpecificationProposals is not { Count: > 0 })
            return Result.Failure<SpecificationProposalSetDetailDto>(
                QcWorksheetErrors.SpecificationProposalHasNoCharacteristics);

        if (!await context.QcWorksheetTemplates.AnyAsync(item => item.Id == request.WorksheetTemplateId))
            return Result.Failure<SpecificationProposalSetDetailDto>(
                QcWorksheetErrors.TemplateNotFound(request.WorksheetTemplateId));

        var payload = new SpecificationProposalPayload
        {
            SpecificationProposals = request.SpecificationProposals,
            SamplingPointGroupProposals = request.SamplingPointGroupProposals ?? [],
            SamplingPointCodes = request.SamplingPointCodes ?? []
        };

        var set = new SpecificationProposalSet
        {
            Id = Guid.NewGuid(),
            Family = family,
            SourceFileName = request.SourceFileName?.Trim(),
            WorksheetTemplateId = request.WorksheetTemplateId,
            ProductName = Blank(request.ProductName),
            SpecificationCode = Blank(request.SpecificationCode),
            ProposalJson = JsonSerializer.Serialize(payload, Json),
            Status = SpecificationProposalStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            CreatedById = userId
        };

        context.QcSpecificationProposalSets.Add(set);
        await context.SaveChangesAsync();

        return await GetProposalSet(set.Id);
    }

    public async Task<Result<List<SpecificationProposalSetSummaryDto>>> GetProposalSets(
        SpecificationProposalStatus? status, ArdFamily? family)
    {
        var query = context.QcSpecificationProposalSets
            .AsNoTracking()
            .Include(item => item.CreatedBy)
            .Include(item => item.WorksheetTemplate)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(item => item.Status == status.Value);

        if (family.HasValue)
            query = query.Where(item => item.Family == family.Value);

        var sets = await query.OrderByDescending(item => item.CreatedAt).ToListAsync();
        return Result.Success(sets.Select(set => (SpecificationProposalSetSummaryDto)ToDetailDto(set)).ToList());
    }

    public async Task<Result<SpecificationProposalSetDetailDto>> GetProposalSet(Guid id)
    {
        var set = await context.QcSpecificationProposalSets
            .AsNoTracking()
            .Include(item => item.CreatedBy)
            .Include(item => item.WorksheetTemplate)
            .SingleOrDefaultAsync(item => item.Id == id);

        return set is null
            ? Result.Failure<SpecificationProposalSetDetailDto>(QcWorksheetErrors.SpecificationProposalSetNotFound(id))
            : Result.Success(ToDetailDto(set));
    }

    public async Task<Result<SpecificationProposalSetDetailDto>> Dismiss(
        Guid id, SpecificationProposalDismissRequest request, Guid userId)
    {
        var set = await context.QcSpecificationProposalSets.SingleOrDefaultAsync(item => item.Id == id);
        if (set is null)
            return Result.Failure<SpecificationProposalSetDetailDto>(
                QcWorksheetErrors.SpecificationProposalSetNotFound(id));

        if (string.IsNullOrWhiteSpace(request?.Reason))
            return Result.Failure<SpecificationProposalSetDetailDto>(
                QcWorksheetErrors.SpecificationProposalDismissReasonRequired);

        if (set.Status != SpecificationProposalStatus.Pending)
            return Result.Failure<SpecificationProposalSetDetailDto>(
                QcWorksheetErrors.SpecificationProposalNotPending(set.SourceFileName, set.Status));

        set.Status = SpecificationProposalStatus.Dismissed;
        set.DismissReason = request.Reason.Trim();
        set.UpdatedAt = DateTime.UtcNow;
        set.LastUpdatedById = userId;
        await context.SaveChangesAsync();

        return await GetProposalSet(id);
    }

    // -----------------------------------------------------------------------
    // Draft plan (read-only)
    // -----------------------------------------------------------------------

    public async Task<Result<SpecificationDraftPlan>> BuildDraftPlan(SpecificationProposalDraftRequest request)
    {
        var loaded = await LoadPendingSets(request?.ProposalSetIds, track: false);
        if (!loaded.IsSuccess)
            return Result.Failure<SpecificationDraftPlan>(loaded.Error);

        var sets = loaded.Value;
        var family = sets[0].Family;
        var plan = new SpecificationDraftPlan { Family = family };

        // The one template every characteristic binds to — or, when appending to an existing
        // Draft EM Specification, that Specification's own microbial link.
        var templateId = sets[0].WorksheetTemplateId;
        HashSet<Guid> existingTierGroupIds = [];

        switch (family)
        {
            case ArdFamily.ProductMicro:
                plan.Code = sets[0].SpecificationCode;
                plan.Name = sets[0].ProductName;
                plan.AppliesTo = SpecificationAppliesTo.Product;
                plan.Stage = SpecificationStage.Finished;
                break;

            case ArdFamily.PurifiedWater:
                plan.Name = WaterSpecificationName;
                plan.AppliesTo = SpecificationAppliesTo.RoutineWater;
                break;

            case ArdFamily.EnvironmentalMonitoring:
                plan.Name = EnvironmentalSpecificationName;
                plan.AppliesTo = SpecificationAppliesTo.RoutineEnvironmental;

                var target = await FindEditableEmSpecification();
                if (target is not null)
                {
                    // One EM Specification: a later upload's tiers are appended to the
                    // existing Draft rather than starting a second one.
                    plan.TargetSpecificationId = target.Id;
                    plan.Code = target.Code;
                    plan.Name = target.Name;
                    plan.WorksheetLinks = target.WorksheetLinks
                        .Select(link => new CreateSpecificationWorksheetLinkRequest
                        {
                            WorksheetTemplateId = link.WorksheetTemplateId,
                            AnalysisType = link.AnalysisType
                        })
                        .ToList();

                    var microbial = target.WorksheetLinks
                        .FirstOrDefault(link => link.AnalysisType == SpecificationAnalysisType.Microbial);
                    if (microbial is not null)
                        templateId = microbial.WorksheetTemplateId;

                    existingTierGroupIds = target.Characteristics
                        .Where(characteristic => characteristic.SamplingPointGroupId.HasValue)
                        .Select(characteristic => characteristic.SamplingPointGroupId!.Value)
                        .ToHashSet();
                }
                else if (await context.QcSpecifications.AnyAsync(item =>
                             item.AppliesTo == SpecificationAppliesTo.RoutineEnvironmental
                             && item.Status == QcDocumentStatus.Effective))
                {
                    plan.Warnings.Add(new SpecificationDraftPlanWarning
                    {
                        Code = SpecificationDraftPlanWarningCodes.EffectiveEmSpecificationExists,
                        Message = "An Effective Environmental Monitoring Specification already exists. "
                                  + "Create a new version of it through the normal flow and apply into "
                                  + "that Draft, rather than creating a second EM Specification."
                    });
                }

                break;
        }

        if (plan.WorksheetLinks.Count == 0)
            plan.WorksheetLinks.Add(new CreateSpecificationWorksheetLinkRequest
            {
                WorksheetTemplateId = templateId,
                AnalysisType = SpecificationAnalysisType.Microbial
            });

        var template = await context.QcWorksheetTemplates
            .AsNoTracking()
            .Include(item => item.Sections)
                .ThenInclude(section => section.Fields)
            .AsSplitQuery()
            .SingleOrDefaultAsync(item => item.Id == templateId);

        if (template is null)
            return Result.Failure<SpecificationDraftPlan>(QcWorksheetErrors.TemplateNotFound(templateId));

        if (template.Status != QcDocumentStatus.Effective)
            plan.Warnings.Add(new SpecificationDraftPlanWarning
            {
                Code = SpecificationDraftPlanWarningCodes.TemplateNotEffective,
                Message = $"Worksheet template '{template.Code}' v{template.Version} is {template.Status}. "
                          + "A Draft Specification may link it, but it must be Effective before testing."
            });

        var fieldKeys = template.Sections
            .SelectMany(section => section.Fields)
            .Select(field => field.FieldKey)
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var payloads = sets.Select(set => (Set: set, Payload: ReadPayload(set))).ToList();
        var liveGroups = await context.QcSamplingPointGroups.AsNoTracking().ToListAsync();

        AddGroups(plan, payloads.SelectMany(item => item.Payload.SamplingPointGroupProposals), liveGroups);

        var rows = new List<SpecificationDraftPlanCharacteristic>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (set, payload) in payloads)
        {
            foreach (var proposal in payload.SpecificationProposals)
            {
                var fieldKey = proposal.SourceFieldKey?.Trim();
                if (string.IsNullOrEmpty(fieldKey) || !fieldKeys.Contains(fieldKey))
                {
                    plan.Warnings.Add(new SpecificationDraftPlanWarning
                    {
                        Code = SpecificationDraftPlanWarningCodes.FieldNotOnTemplate,
                        Message = $"'{proposal.TestName}' from '{set.SourceFileName}' binds to field "
                                  + $"'{fieldKey ?? "(none)"}', which is not on '{template.Code}' "
                                  + $"v{template.Version}. The row was left out."
                    });
                    continue;
                }

                var groupName = Blank(proposal.GroupName);

                // Identical rows from different sets (same tier, same limits) merge into one.
                // A tier is identified by its name and limits alone; the test name printed beside
                // it varies between area sheets without changing what is being judged.
                var signature = string.Join("\u001f", groupName ?? fieldKey,
                    groupName is null ? Normalize(proposal.TestName) : string.Empty,
                    Normalize(proposal.AcceptanceCriteria), Normalize(proposal.AlertLimit), Normalize(proposal.ActionLimit));
                if (!seen.Add(signature))
                    continue;

                // An Alert limit is only ever a suggestion (from a completed COA in the same
                // upload), so a sheet that prints none agrees with one that has it: the rows
                // merge, keeping the suggestion, rather than raising a false TierConflict.
                var compatible = groupName is null
                    ? null
                    : rows.FirstOrDefault(row =>
                        SameName(row.SamplingPointGroupName, groupName)
                        && Normalize(row.AcceptanceCriteria) == Normalize(proposal.AcceptanceCriteria)
                        && Normalize(row.ActionLimit) == Normalize(proposal.ActionLimit)
                        && (row.AlertLimit is null || proposal.AlertLimit is null));
                if (compatible is not null)
                {
                    compatible.AlertLimit ??= proposal.AlertLimit;
                    continue;
                }

                // Appending: a tier the target Specification already carries is not added again.
                if (groupName is not null
                    && liveGroups.FirstOrDefault(group => SameName(group.Name, groupName)) is { } live
                    && existingTierGroupIds.Contains(live.Id))
                    continue;

                if (groupName is not null && plan.Groups.All(group => !SameName(group.Name, groupName)))
                    plan.Groups.Add(NewPlanGroup(groupName, proposal.AcceptanceCriteria, liveGroups));

                rows.Add(new SpecificationDraftPlanCharacteristic
                {
                    TestName = proposal.TestName,
                    Analyte = proposal.Analyte,
                    AcceptanceCriteria = proposal.AcceptanceCriteria,
                    AlertLimit = proposal.AlertLimit,
                    ActionLimit = proposal.ActionLimit,
                    SamplingPointGroupName = groupName,
                    SamplingPointGroupId = groupName is null
                        ? null
                        : liveGroups.FirstOrDefault(group => SameName(group.Name, groupName))?.Id,
                    SourceWorksheetTemplateId = templateId,
                    SourceFieldKey = fieldKey,
                    IncludeOnCoa = true,
                    GroupName = MicrobialGroupName
                });
            }
        }

        // Same tier name, different limits: every candidate stays visible so the reviewer can
        // choose, and apply refuses until one row per tier is left.
        foreach (var conflict in rows
                     .Where(row => row.SamplingPointGroupName is not null)
                     .GroupBy(row => row.SamplingPointGroupName, StringComparer.OrdinalIgnoreCase)
                     .Where(group => group.Count() > 1))
        {
            plan.Warnings.Add(new SpecificationDraftPlanWarning
            {
                Code = SpecificationDraftPlanWarningCodes.TierConflict,
                Message = $"The tier '{conflict.Key}' is proposed with different limits: "
                          + string.Join(" / ", conflict.Select(Limits))
                          + ". Keep one row for it before applying."
            });
        }

        plan.Characteristics = rows
            .Select((row, index) =>
            {
                row.DisplayOrder = index + 1;
                return row;
            })
            .ToList();

        return Result.Success(plan);
    }

    // -----------------------------------------------------------------------
    // Apply (one transaction)
    // -----------------------------------------------------------------------

    public async Task<Result<SpecificationProposalApplyResult>> Apply(
        SpecificationProposalApplyRequest request, Guid userId)
    {
        var plan = request?.Plan;
        if (plan is null)
            return Result.Failure<SpecificationProposalApplyResult>(
                QcWorksheetErrors.SpecificationProposalPlanRequired);

        await using var transaction = context.Database.IsRelational()
            && context.Database.CurrentTransaction is null
            ? await context.Database.BeginTransactionAsync(IsolationLevel.Serializable)
            : null;

        // 1. Concurrency: every set must still be Pending. This is also the double-apply guard.
        var loaded = await LoadPendingSets(request.ProposalSetIds, track: true);
        if (!loaded.IsSuccess)
            return Result.Failure<SpecificationProposalApplyResult>(loaded.Error);

        var sets = loaded.Value;

        // Checked before anything is written, so a plan the reviewer has not finished never
        // leaves groups or point assignments behind. The full M2 rule still runs in create/update.
        if (plan.RetestPolicy is null)
            return Result.Failure<SpecificationProposalApplyResult>(QcWorksheetErrors.RetestPolicyRequired);

        var characteristics = plan.Characteristics ?? [];
        var duplicateTier = characteristics
            .Select(row => Blank(row.SamplingPointGroupName))
            .Where(name => name is not null)
            .GroupBy(name => name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateTier is not null)
            return Result.Failure<SpecificationProposalApplyResult>(
                QcWorksheetErrors.SpecificationProposalTierConflict(duplicateTier.Key));

        var planGroups = (plan.Groups ?? [])
            .Where(group => !string.IsNullOrWhiteSpace(group.Name))
            .ToList();

        var liveGroups = await context.QcSamplingPointGroups.ToListAsync();

        foreach (var name in characteristics.Select(row => Blank(row.SamplingPointGroupName)).Where(name => name is not null))
        {
            if (planGroups.All(group => !SameName(group.Name, name))
                && liveGroups.All(group => !SameName(group.Name, name)))
                return Result.Failure<SpecificationProposalApplyResult>(
                    QcWorksheetErrors.SpecificationProposalGroupNotInPlan(name));
        }

        // 3 (validated first). Every code must name a live point, and no point may silently
        // move between tiers.
        var points = await context.QcSamplingPoints
            .Include(point => point.SamplingPointGroup)
            .ToListAsync();

        var assignments = new List<(SamplingPoint Point, string GroupName)>();
        foreach (var group in planGroups)
        {
            foreach (var code in (group.PointCodes ?? []).Select(Blank).Where(code => code is not null)
                         .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var point = points.FirstOrDefault(item => SameName(item.Code, code));
                if (point is null)
                    return Result.Failure<SpecificationProposalApplyResult>(
                        QcWorksheetErrors.SpecificationProposalSamplingPointNotFound(code));

                if (point.SamplingPointGroup is not null && !SameName(point.SamplingPointGroup.Name, group.Name))
                    return Result.Failure<SpecificationProposalApplyResult>(
                        QcWorksheetErrors.SpecificationProposalSamplingPointGroupChange(
                            point.Code, point.SamplingPointGroup.Name, group.Name.Trim()));

                assignments.Add((point, group.Name.Trim()));
            }
        }

        // 2. Upsert the groups by name.
        var groupIds = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach (var live in liveGroups)
            groupIds.TryAdd(live.Name.Trim(), live.Id);

        foreach (var group in planGroups)
        {
            var name = group.Name.Trim();
            if (groupIds.ContainsKey(name))
                continue;

            var created = new SamplingPointGroup
            {
                Id = Guid.NewGuid(),
                Name = name,
                Description = group.Description,
                CreatedAt = DateTime.UtcNow,
                CreatedById = userId
            };

            context.QcSamplingPointGroups.Add(created);
            groupIds[name] = created.Id;
        }

        // 3. Assign each point to its tier.
        foreach (var (point, groupName) in assignments)
        {
            var groupId = groupIds[groupName];
            if (point.SamplingPointGroupId == groupId)
                continue;

            point.SamplingPointGroupId = groupId;
            point.UpdatedAt = DateTime.UtcNow;
            point.LastUpdatedById = userId;
        }

        await context.SaveChangesAsync();

        // 4. Create, or update the target Draft, through the M2 repository so its validation
        // runs unchanged.
        var rows = characteristics
            .Select(row => new CreateSpecificationCharacteristicRequest
            {
                TestName = row.TestName,
                Analyte = row.Analyte,
                AcceptanceCriteria = row.AcceptanceCriteria,
                AlertLimit = row.AlertLimit,
                ActionLimit = row.ActionLimit,
                SamplingPointGroupId = Blank(row.SamplingPointGroupName) is { } name
                    ? groupIds[name]
                    : row.SamplingPointGroupId,
                SourceWorksheetTemplateId = row.SourceWorksheetTemplateId,
                SourceFieldKey = row.SourceFieldKey,
                IncludeOnCoa = row.IncludeOnCoa,
                DisplayOrder = row.DisplayOrder,
                GroupName = row.GroupName
            })
            .ToList();

        Result<SpecificationDetailDto> saved;

        if (plan.TargetSpecificationId is { } targetId)
        {
            var existing = await specifications.GetSpecification(targetId);
            if (!existing.IsSuccess)
                return Result.Failure<SpecificationProposalApplyResult>(existing.Error);

            // Append only the tiers the target does not already carry; its existing rows are
            // resubmitted as they are, since update replaces the characteristic set whole.
            var existingTierIds = existing.Value.Characteristics
                .Where(characteristic => characteristic.SamplingPointGroupId.HasValue)
                .Select(characteristic => characteristic.SamplingPointGroupId!.Value)
                .ToHashSet();

            var kept = existing.Value.Characteristics
                .Select(characteristic => new CreateSpecificationCharacteristicRequest
                {
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
                    GroupName = characteristic.GroupName
                })
                .ToList();

            var next = kept.Select(row => row.DisplayOrder).DefaultIfEmpty(0).Max();
            foreach (var row in rows.Where(row =>
                         row.SamplingPointGroupId is not { } id || !existingTierIds.Contains(id)))
            {
                row.DisplayOrder = ++next;
                kept.Add(row);
            }

            saved = await specifications.UpdateSpecification(targetId, new UpdateSpecificationRequest
            {
                Code = plan.Code,
                Name = plan.Name,
                AppliesTo = plan.AppliesTo,
                Stage = plan.Stage,
                RetestPolicy = plan.RetestPolicy,
                WorksheetLinks = plan.WorksheetLinks ?? [],
                Characteristics = kept
            }, userId);
        }
        else
        {
            saved = await specifications.CreateSpecification(new CreateSpecificationRequest
            {
                Code = plan.Code,
                Name = plan.Name,
                AppliesTo = plan.AppliesTo,
                Stage = plan.Stage,
                RetestPolicy = plan.RetestPolicy,
                WorksheetLinks = plan.WorksheetLinks ?? [],
                Characteristics = rows
            }, userId);
        }

        // A failure here disposes the transaction uncommitted, so the groups and point
        // assignments above roll back with it.
        if (!saved.IsSuccess)
            return Result.Failure<SpecificationProposalApplyResult>(saved.Error);

        // 5. Mark the sets Applied.
        foreach (var set in sets)
        {
            set.Status = SpecificationProposalStatus.Applied;
            set.AppliedSpecificationId = saved.Value.Id;
            set.UpdatedAt = DateTime.UtcNow;
            set.LastUpdatedById = userId;
        }

        await context.SaveChangesAsync();

        if (transaction is not null)
            await transaction.CommitAsync();

        return Result.Success(new SpecificationProposalApplyResult { SpecificationId = saved.Value.Id });
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    /// <summary>Loads the selected sets and applies the rules shared by draft and apply.</summary>
    private async Task<Result<List<SpecificationProposalSet>>> LoadPendingSets(List<Guid> ids, bool track)
    {
        var distinct = (ids ?? []).Distinct().ToList();
        if (distinct.Count == 0)
            return Result.Failure<List<SpecificationProposalSet>>(QcWorksheetErrors.SpecificationProposalSetsRequired);

        var query = context.QcSpecificationProposalSets.Where(item => distinct.Contains(item.Id));
        var sets = track ? await query.ToListAsync() : await query.AsNoTracking().ToListAsync();

        var missing = distinct.FirstOrDefault(id => sets.All(set => set.Id != id));
        if (missing != Guid.Empty)
            return Result.Failure<List<SpecificationProposalSet>>(
                QcWorksheetErrors.SpecificationProposalSetNotFound(missing));

        var settled = sets.FirstOrDefault(set => set.Status != SpecificationProposalStatus.Pending);
        if (settled is not null)
            return Result.Failure<List<SpecificationProposalSet>>(
                QcWorksheetErrors.SpecificationProposalNotPending(settled.SourceFileName, settled.Status));

        if (sets.Select(set => set.Family).Distinct().Count() > 1)
            return Result.Failure<List<SpecificationProposalSet>>(QcWorksheetErrors.SpecificationProposalMixedFamilies);

        if (sets[0].Family == ArdFamily.ProductMicro && sets.Count > 1)
            return Result.Failure<List<SpecificationProposalSet>>(QcWorksheetErrors.SpecificationProposalProductSingleSet);

        if (sets.Select(set => set.WorksheetTemplateId).Distinct().Count() > 1)
            return Result.Failure<List<SpecificationProposalSet>>(QcWorksheetErrors.SpecificationProposalTemplateMismatch);

        // Oldest first, so the plan reads in upload order.
        return Result.Success(sets.OrderBy(set => set.CreatedAt).ToList());
    }

    /// <summary>The newest Draft/UnderReview EM Specification — the one a later upload appends to.</summary>
    private async Task<Specification> FindEditableEmSpecification() =>
        await context.QcSpecifications
            .AsNoTracking()
            .Include(item => item.WorksheetLinks)
            .Include(item => item.Characteristics)
            .AsSplitQuery()
            .Where(item => item.AppliesTo == SpecificationAppliesTo.RoutineEnvironmental
                           && (item.Status == QcDocumentStatus.Draft
                               || item.Status == QcDocumentStatus.UnderReview))
            .OrderByDescending(item => item.CreatedAt)
            .FirstOrDefaultAsync();

    /// <summary>Groups merge by name across sets; their point codes are unioned.</summary>
    private static void AddGroups(
        SpecificationDraftPlan plan,
        IEnumerable<SamplingPointGroupProposal> proposals,
        List<SamplingPointGroup> liveGroups)
    {
        foreach (var proposal in proposals)
        {
            var name = Blank(proposal.Name);
            if (name is null)
                continue;

            var group = plan.Groups.FirstOrDefault(item => SameName(item.Name, name));
            if (group is null)
            {
                group = NewPlanGroup(name, proposal.AcceptanceCriteria, liveGroups);
                plan.Groups.Add(group);
            }

            foreach (var code in (proposal.PointCodes ?? []).Select(Blank).Where(code => code is not null))
            {
                if (!group.PointCodes.Contains(code, StringComparer.OrdinalIgnoreCase))
                    group.PointCodes.Add(code);
            }
        }
    }

    private static SpecificationDraftPlanGroup NewPlanGroup(
        string name, string criteria, List<SamplingPointGroup> liveGroups)
    {
        var live = liveGroups.FirstOrDefault(group => SameName(group.Name, name));
        return new SpecificationDraftPlanGroup
        {
            Name = live?.Name ?? name,
            Description = live?.Description ?? criteria,
            SamplingPointGroupId = live?.Id,
            IsNew = live is null
        };
    }

    private static string Limits(SpecificationDraftPlanCharacteristic row) =>
        string.Join(", ", new[]
        {
            $"criteria {row.AcceptanceCriteria}",
            row.AlertLimit is null ? null : $"alert {row.AlertLimit}",
            row.ActionLimit is null ? null : $"action {row.ActionLimit}"
        }.Where(part => part is not null));

    private static SpecificationProposalPayload ReadPayload(SpecificationProposalSet set) =>
        (string.IsNullOrWhiteSpace(set.ProposalJson)
            ? null
            : JsonSerializer.Deserialize<SpecificationProposalPayload>(set.ProposalJson, Json))
        ?? new SpecificationProposalPayload();

    private static bool SameName(string left, string right) =>
        string.Equals(left?.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase);

    /// <summary>Case- and whitespace-insensitive, so "NMT 5 cfu/4Hrs" and "NMT 5 CFU/ 4Hrs" compare equal.</summary>
    private static string Normalize(string value) =>
        new string((value ?? string.Empty).Where(character => !char.IsWhiteSpace(character)).ToArray())
            .ToLowerInvariant();

    private static string Blank(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private SpecificationProposalSetDetailDto ToDetailDto(SpecificationProposalSet set)
    {
        var payload = ReadPayload(set);
        return new SpecificationProposalSetDetailDto
        {
            Id = set.Id,
            Family = set.Family,
            SourceFileName = set.SourceFileName,
            WorksheetTemplateId = set.WorksheetTemplateId,
            WorksheetTemplate = set.WorksheetTemplate is null
                ? null
                : new WorksheetTemplateReferenceDto
                {
                    Id = set.WorksheetTemplate.Id,
                    Code = set.WorksheetTemplate.Code,
                    Name = set.WorksheetTemplate.Name,
                    Version = set.WorksheetTemplate.Version,
                    Category = set.WorksheetTemplate.Category,
                    Status = set.WorksheetTemplate.Status
                },
            ProductName = set.ProductName,
            SpecificationCode = set.SpecificationCode,
            Status = set.Status,
            CharacteristicCount = payload.SpecificationProposals.Count,
            TierCount = payload.SamplingPointGroupProposals.Count,
            AppliedSpecificationId = set.AppliedSpecificationId,
            DismissReason = set.DismissReason,
            CreatedAt = set.CreatedAt,
            CreatedBy = mapper.Map<UserDto>(set.CreatedBy),
            SpecificationProposals = payload.SpecificationProposals,
            SamplingPointGroupProposals = payload.SamplingPointGroupProposals,
            SamplingPointCodes = payload.SamplingPointCodes
        };
    }
}
