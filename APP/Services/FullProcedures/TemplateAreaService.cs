using DOMAIN.Entities.FullProcedures;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Services.FullProcedures;

public sealed class TemplateAreaService(ApplicationDbContext context) : ITemplateAreaService
{
    public async Task<Result<TemplateAreaCatalogDto>> GetCatalogAsync(
        CancellationToken cancellationToken = default)
    {
        var (_, dto) = await TemplateAreaCatalogProvider.BuildAsync(context, cancellationToken);
        return dto;
    }

    public async Task<Result<IReadOnlyList<TemplateAreaDto>>> ListAsync(
        IReadOnlyCollection<Guid> actorRoleIds, bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        var roleIds = actorRoleIds.Distinct().ToArray();
        var query = Areas().AsNoTracking().Where(item =>
            roleIds.Contains(item.OwnerRoleId) || item.RoleGrants.Any(grant => roleIds.Contains(grant.RoleId)));
        if (!includeInactive) query = query.Where(item => item.IsActive);
        var areas = await query.OrderBy(item => item.Name).ToListAsync(cancellationToken);
        return Result.Success<IReadOnlyList<TemplateAreaDto>>(areas.Select(TemplateAreaServiceSupport.ToDto).ToArray());
    }

    public async Task<Result<TemplateAreaDto>> GetAsync(
        Guid id, IReadOnlyCollection<Guid> actorRoleIds,
        CancellationToken cancellationToken = default)
    {
        var area = await Areas().AsNoTracking().SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (area is null) return TemplateAreaErrors.NotFound;
        return IsAssigned(area, actorRoleIds) ? TemplateAreaServiceSupport.ToDto(area) : TemplateAreaErrors.AccessDenied;
    }

    public async Task<Result<TemplateAreaDto>> CreateAsync(
        TemplateAreaDraftRequest request, Guid actorId, IReadOnlyCollection<Guid> actorRoleIds,
        CancellationToken cancellationToken = default)
    {
        if (!TemplateAreaServiceSupport.HasReason(request.Reason)) return TemplateAreaErrors.ReasonRequired;
        var validation = await ValidateAsync(request, null, cancellationToken);
        if (validation.IsFailure) return Result.Failure<TemplateAreaDto>(validation.Errors);
        if (!CanCreate(request, actorRoleIds)) return TemplateAreaErrors.AdministratorRequired;

        var area = new TemplateArea
        {
            Id = Guid.NewGuid(), Name = request.Name.Trim(),
            NormalizedName = TemplateAreaServiceSupport.NormalizeName(request.Name), OwnerRoleId = request.OwnerRoleId,
            ReviewPolicyId = request.ReviewPolicyId, Version = 1, IsActive = true,
        };
        TemplateAreaServiceSupport.ReplaceBindings(area, request);
        area.Audits.Add(TemplateAreaServiceSupport.CreateAudit(area, actorId, "Created", request.Reason));
        context.Add(area);
        return await SaveAsync(area, cancellationToken);
    }

    public async Task<Result<TemplateAreaDto>> UpdateAsync(
        Guid id, UpdateTemplateAreaRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, CancellationToken cancellationToken = default)
    {
        if (!TemplateAreaServiceSupport.HasReason(request.Reason)) return TemplateAreaErrors.ReasonRequired;
        var area = await Areas().SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (area is null) return TemplateAreaErrors.NotFound;
        if (!IsAdministrator(area, actorRoleIds)) return TemplateAreaErrors.AdministratorRequired;
        if (request.ExpectedVersion != area.Version) return TemplateAreaErrors.VersionConflict;
        var validation = await ValidateAsync(request, id, cancellationToken);
        if (validation.IsFailure) return Result.Failure<TemplateAreaDto>(validation.Errors);
        var bindingInUse = await context.Set<TemplateQuestion>().AsNoTracking().AnyAsync(item =>
            item.TemplateAreaId == id &&
            (!request.PurposeIds.Contains(item.PurposeId) ||
             !request.SubjectTypeIds.Contains(item.SubjectTypeId)), cancellationToken);
        bindingInUse = bindingInUse || await context.Set<TemplateSection>().AsNoTracking()
            .AnyAsync(item => item.TemplateAreaId == id &&
                (!request.PurposeIds.Contains(item.PurposeId) ||
                 !request.SubjectTypeIds.Contains(item.SubjectTypeId)), cancellationToken);
        bindingInUse = bindingInUse || await context.Set<TemplateForm>().AsNoTracking()
            .AnyAsync(item => item.TemplateAreaId == id &&
                (!request.PurposeIds.Contains(item.PurposeId) ||
                 !request.SubjectTypeIds.Contains(item.SubjectTypeId)), cancellationToken);
        bindingInUse = bindingInUse || await context.Set<TemplateActivity>().AsNoTracking()
            .AnyAsync(item => item.TemplateAreaId == id &&
                (!request.PurposeIds.Contains(item.PurposeId) ||
                 !request.SubjectTypeIds.Contains(item.SubjectTypeId)), cancellationToken);
        bindingInUse = bindingInUse || await context.Set<TemplateActivity>()
            .AsNoTracking().AnyAsync(item => item.TemplateAreaId == id &&
                item.Revisions.SelectMany(revision => revision.Resources).Any(resource =>
                    !request.CapabilityIds.Contains(resource.CapabilityId)), cancellationToken);
        bindingInUse = bindingInUse || await context.Set<TemplateWorkflow>().AsNoTracking()
            .AnyAsync(item => item.TemplateAreaId == id &&
                (!request.PurposeIds.Contains(item.PurposeId) ||
                 !request.SubjectTypeIds.Contains(item.SubjectTypeId)), cancellationToken);
        bindingInUse = bindingInUse || await context.Set<TemplateSharingGrant>().AsNoTracking()
            .AnyAsync(item => item.Status == TemplateSharingGrantStatus.Active &&
                (item.SourceAreaId == id || item.TargetAreaId == id) &&
                (!request.PurposeIds.Contains(item.PurposeId) ||
                 !request.SubjectTypeIds.Contains(item.SubjectTypeId)), cancellationToken);
        if (bindingInUse) return TemplateAreaErrors.Invalid("BindingInUse");

        area.Name = request.Name.Trim();
        area.NormalizedName = TemplateAreaServiceSupport.NormalizeName(request.Name);
        area.OwnerRoleId = request.OwnerRoleId;
        area.ReviewPolicyId = request.ReviewPolicyId;
        context.Entry(area).Property(item => item.Version).OriginalValue = request.ExpectedVersion;
        area.Version++;
        TemplateAreaServiceSupport.ReplaceBindings(area, request);
        context.AddRange(area.Purposes.Cast<object>().Concat(area.SubjectTypes)
            .Concat(area.Capabilities).Concat(area.RoleGrants));
        var audit = TemplateAreaServiceSupport.CreateAudit(area, actorId, "Updated", request.Reason);
        area.Audits.Add(audit);
        context.Add(audit);
        return await SaveAsync(area, cancellationToken);
    }

    public async Task<Result<TemplateAreaDto>> SetActiveAsync(
        Guid id, ChangeTemplateAreaActiveRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, CancellationToken cancellationToken = default)
    {
        if (!TemplateAreaServiceSupport.HasReason(request.Reason)) return TemplateAreaErrors.ReasonRequired;
        var area = await Areas().SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (area is null) return TemplateAreaErrors.NotFound;
        if (!IsAdministrator(area, actorRoleIds)) return TemplateAreaErrors.AdministratorRequired;
        if (request.ExpectedVersion != area.Version) return TemplateAreaErrors.VersionConflict;
        if (area.IsActive == request.IsActive) return TemplateAreaServiceSupport.ToDto(area);

        area.IsActive = request.IsActive;
        context.Entry(area).Property(item => item.Version).OriginalValue = request.ExpectedVersion;
        area.Version++;
        var audit = TemplateAreaServiceSupport.CreateAudit(area, actorId,
            request.IsActive ? "Activated" : "Deactivated", request.Reason);
        area.Audits.Add(audit);
        context.Add(audit);
        return await SaveAsync(area, cancellationToken);
    }

    private async Task<Result> ValidateAsync(
        TemplateAreaDraftRequest request, Guid? editingAreaId, CancellationToken cancellationToken)
    {
        var (catalog, _) = await TemplateAreaCatalogProvider.BuildAsync(context, cancellationToken);
        var existing = await context.Set<TemplateArea>().AsNoTracking()
            .Select(item => new TemplateAreaDefinition(item.Id, item.Name, item.OwnerRoleId,
                Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), item.ReviewPolicyId))
            .ToListAsync(cancellationToken);
        var blockers = TemplateAreaPolicy.ValidateDraft(
            TemplateAreaServiceSupport.ToDraft(request), existing, catalog, editingAreaId);
        if (request.RoleGrants.Select(item => item.RoleId).Distinct().Count() != request.RoleGrants.Count ||
            request.RoleGrants.Any(item => item.RoleId == Guid.Empty ||
                !Enum.IsDefined(item.AccessLevel) || !catalog.OwnerGroupIds.Contains(item.RoleId)))
            blockers = blockers.Append(TemplateAreaBlocker.UnknownOwnerGroup).ToHashSet();
        if (blockers.Count == 0) return Result.Success();
        if (blockers.Contains(TemplateAreaBlocker.DuplicateName)) return TemplateAreaErrors.DuplicateName;
        return Result.Failure(blockers.OrderBy(item => item)
            .Select(item => TemplateAreaErrors.Invalid(item.ToString())).ToList());
    }

    private IQueryable<TemplateArea> Areas() => context.Set<TemplateArea>()
        .Include(item => item.OwnerRole).Include(item => item.Purposes)
        .Include(item => item.SubjectTypes).Include(item => item.Capabilities)
        .Include(item => item.RoleGrants).ThenInclude(item => item.Role);

    private static bool IsAssigned(TemplateArea area, IReadOnlyCollection<Guid> roleIds) =>
        roleIds.Contains(area.OwnerRoleId) || area.RoleGrants.Any(grant => roleIds.Contains(grant.RoleId));

    private static bool IsAdministrator(TemplateArea area, IReadOnlyCollection<Guid> roleIds) =>
        roleIds.Contains(area.OwnerRoleId) || area.RoleGrants.Any(grant =>
            roleIds.Contains(grant.RoleId) && grant.AccessLevel == TemplateAreaAccessLevel.Administrator);

    private static bool CanCreate(TemplateAreaDraftRequest request, IReadOnlyCollection<Guid> roleIds) =>
        roleIds.Contains(request.OwnerRoleId) || request.RoleGrants.Any(grant =>
            roleIds.Contains(grant.RoleId) && grant.AccessLevel == TemplateAreaAccessLevel.Administrator);

    private async Task<Result<TemplateAreaDto>> SaveAsync(
        TemplateArea area, CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            await context.Entry(area).Reference(item => item.OwnerRole).LoadAsync(cancellationToken);
            foreach (var grant in area.RoleGrants)
                await context.Entry(grant).Reference(item => item.Role).LoadAsync(cancellationToken);
            return TemplateAreaServiceSupport.ToDto(area);
        }
        catch (DbUpdateConcurrencyException)
        {
            return TemplateAreaErrors.VersionConflict;
        }
        catch (DbUpdateException)
        {
            return TemplateAreaErrors.DuplicateName;
        }
    }

}
