using APP.Extensions;
using APP.IRepository;
using APP.Services.Pdf;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.Attachments;
using DOMAIN.Entities.QualityAudits;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class QualityAuditRepository(
    ApplicationDbContext context,
    IMapper mapper,
    IPdfService pdfService,
    IHttpContextAccessor httpContextAccessor
) : IQualityAuditRepository
{
    private static Result<QualityAudit> ValidateStatus(QualityAudit audit, params AuditStatus[] allowed)
    {
        if (!allowed.Contains(audit.Status))
            return Error.Validation(
                "QualityAudit.InvalidStatus",
                $"This action is not allowed while the audit is {audit.Status}."
            );

        return audit;
    }

    public async Task<Result<Guid>> CreateAudit(CreateQualityAuditRequest request, Guid userId)
    {
        if (request.ChecklistTemplateId.HasValue)
        {
            var templateExists = await context.AuditChecklistTemplates.AnyAsync(t =>
                t.Id == request.ChecklistTemplateId.Value
            );
            if (!templateExists)
                return Error.NotFound("QualityAudit.TemplateNotFound", "Checklist template not found.");
        }

        var year = DateTime.UtcNow.Year;
        var sequence = await context.QualityAudits.CountAsync(a => a.CreatedAt.Year == year) + 1;

        var audit = mapper.Map<QualityAudit>(request);
        audit.AuditNumber = $"AUD-{year}-{sequence:D4}";
        audit.Status = AuditStatus.Planned;
        audit.CreatedById = userId;
        audit.TeamMembers = request
            .TeamMemberIds.Distinct()
            .Select(memberId => new QualityAuditTeamMember { UserId = memberId })
            .ToList();

        await context.QualityAudits.AddAsync(audit);
        await context.SaveChangesAsync();
        return audit.Id;
    }

    public async Task<Result> UpdateAudit(Guid auditId, UpdateQualityAuditRequest request, Guid userId)
    {
        var audit = await context
            .QualityAudits.Include(a => a.TeamMembers)
            .FirstOrDefaultAsync(a => a.Id == auditId);
        if (audit == null)
            return Error.NotFound("QualityAudit.NotFound", "Quality audit not found.");

        var statusCheck = ValidateStatus(audit, AuditStatus.Planned);
        if (statusCheck.IsFailure)
            return statusCheck.Error;

        audit.Title = request.Title;
        audit.Scope = request.Scope;
        audit.ObjectiveNotes = request.ObjectiveNotes;
        audit.ScheduledStartDate = request.ScheduledStartDate;
        audit.ScheduledEndDate = request.ScheduledEndDate;
        audit.LeadAuditorId = request.LeadAuditorId;
        audit.LastUpdatedById = userId;

        context.QualityAuditTeamMembers.RemoveRange(audit.TeamMembers);
        audit.TeamMembers = request
            .TeamMemberIds.Distinct()
            .Select(memberId => new QualityAuditTeamMember { QualityAuditId = auditId, UserId = memberId })
            .ToList();

        context.QualityAudits.Update(audit);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> StartAudit(Guid auditId, Guid userId)
    {
        var audit = await context.QualityAudits.FirstOrDefaultAsync(a => a.Id == auditId);
        if (audit == null)
            return Error.NotFound("QualityAudit.NotFound", "Quality audit not found.");

        var statusCheck = ValidateStatus(audit, AuditStatus.Planned);
        if (statusCheck.IsFailure)
            return statusCheck.Error;

        audit.Status = AuditStatus.InProgress;
        audit.ActualStartDate = DateTime.UtcNow;
        audit.LastUpdatedById = userId;

        context.QualityAudits.Update(audit);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result<Guid>> RecordChecklistResponse(
        Guid auditId,
        RecordChecklistResponseRequest request,
        Guid userId
    )
    {
        var audit = await context.QualityAudits.FirstOrDefaultAsync(a => a.Id == auditId);
        if (audit == null)
            return Error.NotFound("QualityAudit.NotFound", "Quality audit not found.");

        var statusCheck = ValidateStatus(audit, AuditStatus.InProgress);
        if (statusCheck.IsFailure)
            return statusCheck.Error;

        if (request.TemplateItemId.HasValue)
        {
            var itemBelongsToTemplate = await context.AuditChecklistTemplateItems.AnyAsync(i =>
                i.Id == request.TemplateItemId.Value
                && i.AuditChecklistTemplateId == audit.ChecklistTemplateId
            );
            if (!itemBelongsToTemplate)
                return Error.Validation(
                    "QualityAudit.InvalidTemplateItem",
                    "This checklist item does not belong to the audit's checklist template."
                );
        }
        else if (string.IsNullOrWhiteSpace(request.AdHocQuestionText))
        {
            return Error.Validation(
                "QualityAudit.MissingQuestion",
                "An ad-hoc checklist response requires question text."
            );
        }

        var response = mapper.Map<AuditChecklistResponse>(request);
        response.QualityAuditId = auditId;
        response.RespondedById = userId;
        response.RespondedAt = DateTime.UtcNow;
        response.CreatedById = userId;

        await context.AuditChecklistResponses.AddAsync(response);
        await context.SaveChangesAsync();
        return response.Id;
    }

    public async Task<Result<Guid>> RaiseFinding(Guid auditId, RaiseFindingRequest request, Guid userId)
    {
        var audit = await context.QualityAudits.FirstOrDefaultAsync(a => a.Id == auditId);
        if (audit == null)
            return Error.NotFound("QualityAudit.NotFound", "Quality audit not found.");

        var statusCheck = ValidateStatus(audit, AuditStatus.InProgress, AuditStatus.PendingReport);
        if (statusCheck.IsFailure)
            return statusCheck.Error;

        if (request.ChecklistResponseId.HasValue)
        {
            var responseBelongsToAudit = await context.AuditChecklistResponses.AnyAsync(r =>
                r.Id == request.ChecklistResponseId.Value && r.QualityAuditId == auditId
            );
            if (!responseBelongsToAudit)
                return Error.Validation(
                    "QualityAudit.InvalidChecklistResponse",
                    "This checklist response does not belong to the audit."
                );
        }

        var finding = mapper.Map<AuditFinding>(request);
        finding.QualityAuditId = auditId;
        finding.RaisedById = userId;
        finding.RaisedAt = DateTime.UtcNow;
        finding.Status = FindingStatus.Open;
        finding.CreatedById = userId;

        await context.AuditFindings.AddAsync(finding);
        await context.SaveChangesAsync();
        return finding.Id;
    }

    public async Task<Result<Guid>> RaiseCorrectiveAction(
        Guid findingId,
        RaiseCorrectiveActionRequest request,
        Guid userId
    )
    {
        var finding = await context
            .AuditFindings.Include(f => f.CorrectiveAction)
            .FirstOrDefaultAsync(f => f.Id == findingId);
        if (finding == null)
            return Error.NotFound("QualityAudit.FindingNotFound", "Audit finding not found.");

        if (finding.CorrectiveAction != null)
            return Error.Validation(
                "QualityAudit.CapaAlreadyExists",
                "A corrective action has already been raised for this finding."
            );

        var capa = mapper.Map<AuditCorrectiveAction>(request);
        capa.AuditFindingId = findingId;
        capa.Status = CapaStatus.Open;
        capa.CreatedById = userId;

        finding.Status = FindingStatus.CapaRaised;
        context.AuditFindings.Update(finding);

        await context.AuditCorrectiveActions.AddAsync(capa);
        await context.SaveChangesAsync();
        return capa.Id;
    }

    public async Task<Result> UpdateCorrectiveActionStatus(
        Guid capaId,
        UpdateCorrectiveActionStatusRequest request,
        Guid userId
    )
    {
        var capa = await context.AuditCorrectiveActions.FirstOrDefaultAsync(c => c.Id == capaId);
        if (capa == null)
            return Error.NotFound("QualityAudit.CapaNotFound", "Corrective action not found.");

        if (request.Status is CapaStatus.Closed)
            return Error.Validation(
                "QualityAudit.CapaCloseRequiresVerification",
                "Closing a corrective action requires verifying its effectiveness."
            );

        capa.Status = request.Status;
        capa.LastUpdatedById = userId;

        context.AuditCorrectiveActions.Update(capa);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> VerifyCorrectiveActionEffectiveness(
        Guid capaId,
        VerifyCorrectiveActionEffectivenessRequest request,
        Guid userId
    )
    {
        var capa = await context
            .AuditCorrectiveActions.Include(c => c.AuditFinding)
            .FirstOrDefaultAsync(c => c.Id == capaId);
        if (capa == null)
            return Error.NotFound("QualityAudit.CapaNotFound", "Corrective action not found.");

        capa.EffectivenessCheckNotes = request.EffectivenessCheckNotes;
        capa.EffectivenessVerifiedById = userId;
        capa.EffectivenessVerifiedAt = DateTime.UtcNow;
        capa.LastUpdatedById = userId;

        if (request.Effective)
        {
            capa.Status = CapaStatus.Closed;
            capa.ClosedAt = DateTime.UtcNow;

            capa.AuditFinding.Status = FindingStatus.Closed;
            context.AuditFindings.Update(capa.AuditFinding);
        }
        else
        {
            capa.Status = CapaStatus.InProgress;
        }

        context.AuditCorrectiveActions.Update(capa);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> SubmitForClosure(Guid auditId, SubmitAuditForClosureRequest request, Guid userId)
    {
        var audit = await context
            .QualityAudits.Include(a => a.Findings)
            .ThenInclude(f => f.CorrectiveAction)
            .FirstOrDefaultAsync(a => a.Id == auditId);
        if (audit == null)
            return Error.NotFound("QualityAudit.NotFound", "Quality audit not found.");

        var statusCheck = ValidateStatus(audit, AuditStatus.InProgress);
        if (statusCheck.IsFailure)
            return statusCheck.Error;

        var openCapaCount = audit.Findings.Count(f =>
            f.CorrectiveAction != null && f.CorrectiveAction.Status != CapaStatus.Closed
        );

        if (openCapaCount > 0 && !request.AcceptOpenCapaDeferral)
            return Error.Validation(
                "QualityAudit.OpenCorrectiveActions",
                $"{openCapaCount} corrective action(s) are still open. Close them, or explicitly defer with a justification."
            );

        if (openCapaCount > 0 && string.IsNullOrWhiteSpace(request.DeferralJustification))
            return Error.Validation(
                "QualityAudit.DeferralJustificationRequired",
                "A justification is required to defer open corrective actions past closure."
            );

        audit.Status = AuditStatus.PendingClosure;
        audit.ActualEndDate = DateTime.UtcNow;
        audit.ClosingMeetingNotes = openCapaCount > 0
            ? $"{request.ClosingMeetingNotes}\n\nDeferral justification: {request.DeferralJustification}"
            : request.ClosingMeetingNotes;
        audit.LastUpdatedById = userId;

        context.QualityAudits.Update(audit);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> CloseAudit(Guid auditId, CloseQualityAuditRequest request, Guid userId)
    {
        var audit = await context.QualityAudits.FirstOrDefaultAsync(a => a.Id == auditId);
        if (audit == null)
            return Error.NotFound("QualityAudit.NotFound", "Quality audit not found.");

        var statusCheck = ValidateStatus(audit, AuditStatus.PendingClosure);
        if (statusCheck.IsFailure)
            return statusCheck.Error;

        if (request.Approve)
        {
            audit.Status = AuditStatus.Closed;
            audit.ClosedAt = DateTime.UtcNow;
            audit.ClosedById = userId;
            audit.IsVerified = true;
            audit.VerifiedAt = DateTime.UtcNow;
            audit.VerifiedById = userId;
        }
        else
        {
            audit.Status = AuditStatus.InProgress;
            audit.ClosingMeetingNotes = $"{audit.ClosingMeetingNotes}\n\nClosure rejected: {request.Comments}";
        }

        audit.LastUpdatedById = userId;
        context.QualityAudits.Update(audit);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    private IQueryable<QualityAudit> AuditDetailQuery() =>
        context
            .QualityAudits.AsSplitQuery()
            .Include(a => a.LeadAuditor)
            .Include(a => a.ClosedBy)
            .Include(a => a.VerifiedBy)
            .Include(a => a.ChecklistTemplate)
            .Include(a => a.TeamMembers)
            .ThenInclude(m => m.User)
            .Include(a => a.ChecklistResponses)
            .ThenInclude(r => r.TemplateItem)
            .Include(a => a.ChecklistResponses)
            .ThenInclude(r => r.RespondedBy)
            .Include(a => a.Findings)
            .ThenInclude(f => f.RaisedBy)
            .Include(a => a.Findings)
            .ThenInclude(f => f.CorrectiveAction)
            .ThenInclude(c => c.ResponsiblePerson)
            .Include(a => a.Findings)
            .ThenInclude(f => f.CorrectiveAction)
            .ThenInclude(c => c.EffectivenessVerifiedBy);

    private QualityAuditDto MapAuditBase(QualityAudit audit)
    {
        var dto = mapper.Map<QualityAuditDto>(audit);
        dto.ChecklistTemplateName = audit.ChecklistTemplate?.Name;
        dto.TeamMembers = audit.TeamMembers.Select(m => mapper.Map<UserDto>(m.User)).ToList();
        return dto;
    }

    /// <summary>
    /// Attachments are populated in one bulk query per page/detail call (rather than via the
    /// shared AutoMapper attachment resolver) because a single audit graph spans three distinct
    /// ModelType values (audit, checklist response, finding), which the resolver's single
    /// per-map ModelType context item can't express.
    /// </summary>
    private async Task PopulateAttachments(params QualityAuditDto[] dtos)
    {
        var relevantIds = dtos
            .SelectMany(dto =>
                new[] { dto.Id }
                    .Concat(dto.ChecklistResponses.Select(r => r.Id))
                    .Concat(dto.Findings.Select(f => f.Id))
            )
            .ToList();

        if (relevantIds.Count == 0)
            return;

        var host = httpContextAccessor.HttpContext?.Request.Host;
        var attachments = await context.Attachments.Where(a => relevantIds.Contains(a.ModelId)).ToListAsync();
        var byModelId = attachments.ToLookup(a => a.ModelId);

        List<AttachmentDto> ToDtos(Guid modelId) =>
            byModelId[modelId]
                .Select(a => new AttachmentDto
                {
                    Name = a.Name,
                    Link = $"http://{host}/api/v1/file/{a.ModelType.ToLower()}/{a.ModelId}/{a.Reference}",
                    Id = a.ModelId,
                    Reference = a.Reference,
                })
                .ToList();

        foreach (var dto in dtos)
        {
            dto.Attachments = ToDtos(dto.Id);
            foreach (var response in dto.ChecklistResponses)
                response.Attachments = ToDtos(response.Id);
            foreach (var finding in dto.Findings)
                finding.Attachments = ToDtos(finding.Id);
        }
    }

    public async Task<Result<QualityAuditDto>> GetAudit(Guid id)
    {
        var audit = await AuditDetailQuery().FirstOrDefaultAsync(a => a.Id == id);
        if (audit == null)
            return Error.NotFound("QualityAudit.NotFound", "Quality audit not found.");

        var dto = MapAuditBase(audit);
        await PopulateAttachments(dto);
        return dto;
    }

    public async Task<Result<Paginateable<IEnumerable<QualityAuditDto>>>> GetAudits(
        int page,
        int pageSize,
        string searchQuery,
        AuditStatus? status,
        AuditType? type
    )
    {
        var query = AuditDetailQuery().AsQueryable();

        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);

        if (type.HasValue)
            query = query.Where(a => a.Type == type.Value);

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.WhereSearch(searchQuery, a => a.AuditNumber, a => a.Title, a => a.Scope);
        }

        query = query.OrderByDescending(a => a.CreatedAt);

        var paginated = await PaginationHelper.GetPaginatedResultAsync(query, page, pageSize, MapAuditBase);
        await PopulateAttachments(paginated.Data.ToArray());
        return paginated;
    }

    public async Task<Result<List<AuditChecklistTemplateDto>>> GetChecklistTemplates()
    {
        var templates = await context
            .AuditChecklistTemplates.AsSplitQuery()
            .Include(t => t.Items.OrderBy(i => i.Order))
            .Where(t => t.IsActive)
            .OrderBy(t => t.Name)
            .ToListAsync();

        return mapper.Map<List<AuditChecklistTemplateDto>>(templates);
    }

    public async Task<Result<byte[]>> GenerateAuditReportPdf(Guid auditId)
    {
        var audit = await AuditDetailQuery().FirstOrDefaultAsync(a => a.Id == auditId);
        if (audit == null)
            return Error.NotFound("QualityAudit.NotFound", "Quality audit not found.");

        var html = PdfTemplate.QualityAuditReportTemplate(MapAuditBase(audit));
        return pdfService.GeneratePdfFromHtml(html);
    }
}
