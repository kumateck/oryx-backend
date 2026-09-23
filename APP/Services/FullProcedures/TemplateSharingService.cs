using DOMAIN.Entities.FullProcedures;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Services.FullProcedures;

public sealed partial class TemplateSharingService
{
    public async Task<Result<IReadOnlyList<TemplateSharingGrantDto>>> ListAsync(Guid areaId,
        IReadOnlyCollection<Guid> actorRoleIds, CancellationToken cancellationToken = default)
    {
        var area = await LoadAreaAsync(areaId, cancellationToken);
        if (area is null) return TemplateSharingErrors.NotFound;
        if (!TemplateDefinitionAuthorization.Allows(
                area, actorRoleIds, TemplateDefinitionOperation.View))
            return TemplateSharingErrors.AccessDenied;
        var grants = await GrantQuery().AsNoTracking()
            .Where(x => x.SourceAreaId == areaId || x.TargetAreaId == areaId)
            .OrderByDescending(x => x.RequestedAt).ToListAsync(cancellationToken);
        return Result.Success<IReadOnlyList<TemplateSharingGrantDto>>(
            grants.Select(TemplateSharingServiceSupport.ToDto).ToArray());
    }

    public async Task<Result<TemplateSharingGrantDto>> RequestAsync(
        RequestTemplateSharingGrantRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        if (!ValidRequest(request)) return TemplateSharingErrors.Invalid;
        var source = await LoadAreaAsync(request.SourceAreaId, cancellationToken);
        var target = await LoadAreaAsync(request.TargetAreaId, cancellationToken);
        if (source is null || target is null) return TemplateSharingErrors.NotFound;
        if (!source.IsActive || !target.IsActive) return TemplateSharingErrors.Conflict;
        if (!CanRequest(request.RequestedByAreaId, source, target, actorRoleIds))
            return TemplateSharingErrors.AccessDenied;
        var revision = await ResolveRevisionAsync(request.TemplateKind,
            request.DefinitionId, request.RevisionId, cancellationToken);
        if (revision is null || revision.AreaId != source.Id || !revision.IsPublished)
            return TemplateSharingErrors.DependencyUnavailable;
        if (!ContextAllowed(target, revision)) return TemplateSharingErrors.Invalid;
        var openExists = await context.Set<TemplateSharingGrant>().AsNoTracking().AnyAsync(x =>
            x.SourceAreaId == source.Id && x.TargetAreaId == target.Id &&
            x.TemplateKind == request.TemplateKind && x.DefinitionId == request.DefinitionId &&
            x.RevisionId == request.RevisionId &&
            (x.Status == TemplateSharingGrantStatus.Pending ||
             x.Status == TemplateSharingGrantStatus.Active), cancellationToken);
        if (openExists) return TemplateSharingErrors.Conflict;
        var now = DateTime.UtcNow;
        var grant = new TemplateSharingGrant
        {
            Id = Guid.NewGuid(), SourceAreaId = source.Id, SourceArea = source,
            TargetAreaId = target.Id, TargetArea = target,
            RequestedByAreaId = request.RequestedByAreaId,
            TemplateKind = request.TemplateKind, DefinitionId = request.DefinitionId,
            RevisionId = request.RevisionId, RevisionContentHash = revision.ContentHash,
            PurposeId = revision.PurposeId, SubjectTypeId = revision.SubjectTypeId,
            Status = TemplateSharingGrantStatus.Pending, Version = 1,
            RequestedById = actorId, RequestedAt = now, CreatedById = actorId,
        };
        grant.Audits.Add(TemplateSharingServiceSupport.Audit(grant, null,
            "Requested", request.Reason, actorId, correlationId));
        context.Add(grant);
        return await SaveAsync(grant.Id, cancellationToken);
    }

    public Task<Result<TemplateSharingGrantDto>> ApproveAsync(Guid grantId,
        DecideTemplateSharingGrantRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default) => DecideAsync(grantId, request,
            actorId, actorRoleIds, correlationId, true, cancellationToken);

    public Task<Result<TemplateSharingGrantDto>> RejectAsync(Guid grantId,
        DecideTemplateSharingGrantRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default) => DecideAsync(grantId, request,
            actorId, actorRoleIds, correlationId, false, cancellationToken);

    public async Task<Result<TemplateSharingGrantDto>> RevokeAsync(Guid grantId,
        DecideTemplateSharingGrantRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        var grant = await GrantQuery().SingleOrDefaultAsync(x => x.Id == grantId, cancellationToken);
        if (grant is null) return TemplateSharingErrors.NotFound;
        if (!TemplateSharingServiceSupport.HasReason(request.Reason) ||
            request.ExpectedVersion != grant.Version ||
            grant.Status != TemplateSharingGrantStatus.Active)
            return TemplateSharingErrors.Conflict;
        if (!CanPublishEither(grant, actorRoleIds)) return TemplateSharingErrors.AccessDenied;
        var prior = grant.Status;
        context.Entry(grant).Property(x => x.Version).OriginalValue = request.ExpectedVersion;
        grant.Status = TemplateSharingGrantStatus.Revoked;
        grant.Version++;
        grant.RevokedById = actorId;
        grant.RevokedAt = DateTime.UtcNow;
        AddAudit(grant, prior, "Revoked", request.Reason, actorId, correlationId);
        return await SaveAsync(grant.Id, cancellationToken);
    }

    public async Task<Result<TemplateRevisionUsageDto>> GetUsageAsync(TemplateRevisionKind kind,
        Guid definitionId, Guid revisionId, IReadOnlyCollection<Guid> actorRoleIds,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(kind) || definitionId == Guid.Empty || revisionId == Guid.Empty)
            return TemplateSharingErrors.Invalid;
        var revision = await ResolveRevisionAsync(kind, definitionId, revisionId, cancellationToken);
        if (revision is null) return TemplateSharingErrors.NotFound;
        var area = await LoadAreaAsync(revision.AreaId, cancellationToken);
        if (area is null) return TemplateSharingErrors.NotFound;
        if (!TemplateDefinitionAuthorization.Allows(
                area, actorRoleIds, TemplateDefinitionOperation.View))
            return TemplateSharingErrors.AccessDenied;
        var dependencies = await CountDirectReferencesAsync(kind, revisionId, cancellationToken);
        var grants = await context.Set<TemplateSharingGrant>().AsNoTracking().CountAsync(x =>
            x.TemplateKind == kind && x.DefinitionId == definitionId &&
            x.RevisionId == revisionId && x.Status == TemplateSharingGrantStatus.Active,
            cancellationToken);
        return new TemplateRevisionUsageDto(kind, definitionId, revisionId, revision.AreaId,
            revision.PurposeId, revision.SubjectTypeId, dependencies, grants);
    }

    private async Task<Result<TemplateSharingGrantDto>> DecideAsync(Guid grantId,
        DecideTemplateSharingGrantRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId, bool approve,
        CancellationToken token)
    {
        var grant = await GrantQuery().SingleOrDefaultAsync(x => x.Id == grantId, token);
        if (grant is null) return TemplateSharingErrors.NotFound;
        if (!TemplateSharingServiceSupport.HasReason(request.Reason) ||
            request.ExpectedVersion != grant.Version ||
            grant.Status != TemplateSharingGrantStatus.Pending)
            return TemplateSharingErrors.Conflict;
        if (grant.RequestedById == actorId) return TemplateSharingErrors.SegregationOfDuties;
        if (!CanDecide(grant, actorRoleIds)) return TemplateSharingErrors.AccessDenied;
        if (approve)
        {
            if (!grant.SourceArea.IsActive || !grant.TargetArea.IsActive)
                return TemplateSharingErrors.DependencyUnavailable;
            var revision = await ResolveRevisionAsync(grant.TemplateKind,
                grant.DefinitionId, grant.RevisionId, token);
            if (revision is null || !revision.IsPublished || revision.AreaId != grant.SourceAreaId ||
                revision.ContentHash != grant.RevisionContentHash ||
                !ContextAllowed(grant.TargetArea, revision))
                return TemplateSharingErrors.DependencyUnavailable;
        }
        var prior = grant.Status;
        context.Entry(grant).Property(x => x.Version).OriginalValue = request.ExpectedVersion;
        grant.Status = approve ? TemplateSharingGrantStatus.Active : TemplateSharingGrantStatus.Rejected;
        grant.Version++;
        grant.DecidedById = actorId;
        grant.DecidedAt = DateTime.UtcNow;
        AddAudit(grant, prior, approve ? "Approved" : "Rejected", request.Reason,
            actorId, correlationId);
        return await SaveAsync(grant.Id, token);
    }

    private static bool ValidRequest(RequestTemplateSharingGrantRequest request) =>
        TemplateSharingServiceSupport.HasReason(request.Reason) &&
        request.SourceAreaId != Guid.Empty && request.TargetAreaId != Guid.Empty &&
        request.SourceAreaId != request.TargetAreaId &&
        request.RequestedByAreaId is var areaId &&
        (areaId == request.SourceAreaId || areaId == request.TargetAreaId) &&
        request.DefinitionId != Guid.Empty && request.RevisionId != Guid.Empty &&
        Enum.IsDefined(request.TemplateKind);

    private static bool CanRequest(Guid requestingAreaId, TemplateArea source,
        TemplateArea target, IReadOnlyCollection<Guid> roles) => requestingAreaId == source.Id
            ? TemplateDefinitionAuthorization.Allows(source, roles, TemplateDefinitionOperation.Publish)
            : TemplateDefinitionAuthorization.Allows(target, roles, TemplateDefinitionOperation.Author);

    private static bool CanDecide(TemplateSharingGrant grant, IReadOnlyCollection<Guid> roles) =>
        grant.RequestedByAreaId == grant.SourceAreaId
            ? TemplateDefinitionAuthorization.Allows(grant.TargetArea, roles,
                TemplateDefinitionOperation.Author)
            : TemplateDefinitionAuthorization.Allows(grant.SourceArea, roles,
                TemplateDefinitionOperation.Publish);

    private static bool CanPublishEither(TemplateSharingGrant grant,
        IReadOnlyCollection<Guid> roles) =>
        TemplateDefinitionAuthorization.Allows(grant.SourceArea, roles,
            TemplateDefinitionOperation.Publish) ||
        TemplateDefinitionAuthorization.Allows(grant.TargetArea, roles,
            TemplateDefinitionOperation.Publish);

    private void AddAudit(TemplateSharingGrant grant, TemplateSharingGrantStatus prior,
        string action, string reason, Guid actorId, Guid correlationId)
    {
        var audit = TemplateSharingServiceSupport.Audit(grant, prior, action,
            reason, actorId, correlationId);
        grant.Audits.Add(audit);
        context.Add(audit);
    }

    private async Task<Result<TemplateSharingGrantDto>> SaveAsync(Guid grantId,
        CancellationToken token)
    {
        try { await context.SaveChangesAsync(token); }
        catch (DbUpdateConcurrencyException) { return TemplateSharingErrors.Conflict; }
        catch (DbUpdateException) { return TemplateSharingErrors.Conflict; }
        context.ChangeTracker.Clear();
        var saved = await GrantQuery().AsNoTracking()
            .SingleAsync(x => x.Id == grantId, token);
        return TemplateSharingServiceSupport.ToDto(saved);
    }
}
