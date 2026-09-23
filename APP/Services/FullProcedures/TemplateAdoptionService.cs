using System.Data;
using DOMAIN.Entities.FullProcedures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SHARED;

namespace APP.Services.FullProcedures;

public sealed partial class TemplateAdoptionService
{
    public async Task<Result<TemplateAdoptionDto>> GetAsync(Guid adoptionId,
        IReadOnlyCollection<Guid> actorRoleIds, CancellationToken cancellationToken = default)
    {
        var item = await context.Set<TemplateAdoption>().AsNoTracking()
            .Include(x => x.TargetArea).ThenInclude(x => x.RoleGrants)
            .SingleOrDefaultAsync(x => x.Id == adoptionId, cancellationToken);
        if (item is null) return TemplateAdoptionErrors.NotFound;
        return TemplateDefinitionAuthorization.Allows(item.TargetArea, actorRoleIds,
            TemplateDefinitionOperation.View)
            ? TemplateAdoptionServiceSupport.ToDto(item)
            : TemplateAdoptionErrors.AccessDenied;
    }

    public async Task<Result<TemplateAdoptionDto>> AdoptAsync(Guid grantId,
        AdoptTemplateRevisionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        if (grantId == Guid.Empty || actorId == Guid.Empty || correlationId == Guid.Empty ||
            !TemplateAdoptionServiceSupport.Valid(request)) return TemplateAdoptionErrors.Invalid;
        var grant = await LoadGrantAsync(grantId, cancellationToken);
        var gate = await ValidateGrantAsync(grant, request.ExpectedGrantVersion,
            actorRoleIds, cancellationToken);
        if (gate is not null) return gate;
        await using var transaction = await BeginTransactionAsync(cancellationToken);
        context.ChangeTracker.Clear();
        grant = await LoadGrantAsync(grantId, cancellationToken);
        gate = await ValidateGrantAsync(grant, request.ExpectedGrantVersion,
            actorRoleIds, cancellationToken);
        if (gate is not null)
        {
            if (transaction is not null) await transaction.RollbackAsync(cancellationToken);
            return gate;
        }
        var draftRequest = await BuildDraftRequestAsync(grant, request, cancellationToken);
        if (draftRequest is null)
        {
            if (transaction is not null) await transaction.RollbackAsync(cancellationToken);
            return TemplateAdoptionErrors.DependencyUnavailable;
        }
        var draft = await CreateTargetDraftAsync(draftRequest, actorId, actorRoleIds,
            correlationId, cancellationToken);
        if (draft.IsFailure)
        {
            if (transaction is not null) await transaction.RollbackAsync(cancellationToken);
            return TemplateAdoptionErrors.DependencyUnavailable;
        }

        context.ChangeTracker.Clear();
        var current = await LoadGrantAsync(grantId, cancellationToken);
        var currentGate = await ValidateGrantAsync(current, request.ExpectedGrantVersion,
            actorRoleIds, cancellationToken);
        if (currentGate is not null)
        {
            if (transaction is not null) await transaction.RollbackAsync(cancellationToken);
            return currentGate;
        }
        context.Entry(current).Property(x => x.Version).OriginalValue = request.ExpectedGrantVersion;
        var prior = current.Status;
        current.Version++;
        var audit = TemplateSharingServiceSupport.Audit(current, prior, "Adopted",
            request.Reason, actorId, correlationId);
        current.Audits.Add(audit);
        context.Add(audit);
        var adoption = TemplateAdoptionServiceSupport.New(
            current, draft.Value, request, actorId, correlationId);
        context.Add(adoption);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            if (transaction is not null) await transaction.RollbackAsync(cancellationToken);
            return TemplateAdoptionErrors.Conflict;
        }
        catch (DbUpdateException)
        {
            if (transaction is not null) await transaction.RollbackAsync(cancellationToken);
            return TemplateAdoptionErrors.Conflict;
        }
        context.ChangeTracker.Clear();
        var saved = await context.Set<TemplateAdoption>().AsNoTracking()
            .SingleAsync(x => x.Id == adoption.Id, cancellationToken);
        return TemplateAdoptionServiceSupport.ToDto(saved);
    }

    private async Task<Error> ValidateGrantAsync(TemplateSharingGrant grant, int expectedVersion,
        IReadOnlyCollection<Guid> roles, CancellationToken token)
    {
        if (grant is null) return TemplateAdoptionErrors.NotFound;
        if (grant.Version != expectedVersion || grant.Status != TemplateSharingGrantStatus.Active ||
            await context.Set<TemplateAdoption>().AsNoTracking()
                .AnyAsync(x => x.TemplateSharingGrantId == grant.Id, token))
            return TemplateAdoptionErrors.Conflict;
        if (!grant.SourceArea.IsActive || !grant.TargetArea.IsActive)
            return TemplateAdoptionErrors.DependencyUnavailable;
        return TemplateDefinitionAuthorization.Allows(grant.TargetArea, roles,
            TemplateDefinitionOperation.Author) ? null : TemplateAdoptionErrors.AccessDenied;
    }

    private async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken token) =>
        context.Database.IsRelational()
            ? await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, token)
            : null;

    private Task<AdoptionDraftRequest> BuildDraftRequestAsync(TemplateSharingGrant grant,
        AdoptTemplateRevisionRequest request, CancellationToken token) => grant.TemplateKind switch
        {
            TemplateRevisionKind.Question => BuildQuestionAsync(grant, request, token),
            TemplateRevisionKind.Section => BuildSectionAsync(grant, request, token),
            TemplateRevisionKind.Form => BuildFormAsync(grant, request, token),
            TemplateRevisionKind.Activity => BuildActivityAsync(grant, request, token),
            TemplateRevisionKind.Workflow => BuildWorkflowAsync(grant, request, token),
            _ => Task.FromResult<AdoptionDraftRequest>(null),
        };

    private async Task<Result<AdoptedDraft>> CreateTargetDraftAsync(AdoptionDraftRequest item,
        Guid actorId, IReadOnlyCollection<Guid> roles, Guid correlationId, CancellationToken token)
    {
        return item.Kind switch
        {
            TemplateRevisionKind.Question => From(await questions.CreateAsync(
                (CreateTemplateQuestionRequest)item.Request, actorId, roles, correlationId, token)),
            TemplateRevisionKind.Section => From(await sections.CreateAsync(
                (CreateTemplateSectionRequest)item.Request, actorId, roles, correlationId, token)),
            TemplateRevisionKind.Form => From(await forms.CreateAsync(
                (CreateTemplateFormRequest)item.Request, actorId, roles, correlationId, token)),
            TemplateRevisionKind.Activity => From(await activities.CreateAsync(
                (CreateTemplateActivityRequest)item.Request, actorId, roles, correlationId, token)),
            TemplateRevisionKind.Workflow => From(await workflows.CreateAsync(
                (CreateTemplateWorkflowRequest)item.Request, actorId, roles, correlationId, token)),
            _ => TemplateAdoptionErrors.Invalid,
        };
    }
}
