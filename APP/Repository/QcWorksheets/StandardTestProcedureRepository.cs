using APP.IRepository;
using APP.Services.QcWorksheets;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.QcWorksheets;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository.QcWorksheets;

public class StandardTestProcedureRepository(
    ApplicationDbContext context,
    IMapper mapper,
    IQcSignatureService signatureService,
    IQcReauthContext reauthContext,
    IApprovalRepository approvalRepository,
    IStpDocxImportService importService) : IStandardTestProcedureRepository
{
    private const string ModelType = QcWorksheetModelTypes.StandardTestProcedure;

    // -----------------------------------------------------------------------
    // Reads
    // -----------------------------------------------------------------------

    public async Task<Result<Paginateable<IEnumerable<StpSummaryDto>>>> GetStps(
        int page, int pageSize, string searchQuery, QcDocumentStatus? status)
    {
        var query = context.QcStandardTestProcedures
            .Include(item => item.CreatedBy)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            var term = searchQuery.Trim().ToLower();
            query = query.Where(item =>
                item.Code.ToLower().Contains(term)
                || item.Name.ToLower().Contains(term)
                || (item.Area != null && item.Area.ToLower().Contains(term)));
        }

        if (status.HasValue)
            query = query.Where(item => item.Status == status.Value);

        var result = await PaginationHelper.GetPaginatedResultAsync(
            query.OrderByDescending(item => item.CreatedAt),
            page,
            pageSize,
            ToSummaryDto);

        return result;
    }

    public async Task<Result<StpDetailDto>> GetStp(Guid id)
    {
        var stp = await LoadDetail(id);
        return stp is null
            ? Result.Failure<StpDetailDto>(QcWorksheetErrors.StpNotFound(id))
            : Result.Success(ToDetailDto(stp));
    }

    // -----------------------------------------------------------------------
    // Authoring
    // -----------------------------------------------------------------------

    public async Task<Result<StpDetailDto>> CreateStp(CreateStpRequest request, Guid userId)
    {
        var referenceCheck = await ValidateStepReferences(request.Steps);
        if (!referenceCheck.IsSuccess)
            return Result.Failure<StpDetailDto>(referenceCheck.Error);

        var stp = new StandardTestProcedure
        {
            Id = Guid.NewGuid(),
            Code = request.Code?.Trim(),
            Name = request.Name?.Trim(),
            Area = request.Area?.Trim(),
            Purpose = request.Purpose,
            Scope = request.Scope,
            Responsibility = request.Responsibility,
            Accountability = request.Accountability,
            ReviewDate = request.ReviewDate,
            IssueDate = request.IssueDate,
            Version = 1,
            Status = QcDocumentStatus.Draft,
            Approved = false,
            CreatedAt = DateTime.UtcNow,
            CreatedById = userId,
            Steps = BuildSteps(request.Steps, userId)
        };

        context.QcStandardTestProcedures.Add(stp);
        await context.SaveChangesAsync();

        return await GetStp(stp.Id);
    }

    public async Task<Result<StpDetailDto>> UpdateStp(Guid id, UpdateStpRequest request, Guid userId)
    {
        var stp = await LoadForUpdate(id);
        if (stp is null)
            return Result.Failure<StpDetailDto>(QcWorksheetErrors.StpNotFound(id));

        // Edit-triggers-versioning, enforced server-side. Client-side read-only rendering
        // is never trusted on its own.
        if (stp.Status is not (QcDocumentStatus.Draft or QcDocumentStatus.UnderReview))
            return Result.Failure<StpDetailDto>(QcWorksheetErrors.NotEditableInStatus(stp.Status));

        var referenceCheck = await ValidateStepReferences(request.Steps, id);
        if (!referenceCheck.IsSuccess)
            return Result.Failure<StpDetailDto>(referenceCheck.Error);

        stp.Code = request.Code?.Trim();
        stp.Name = request.Name?.Trim();
        stp.Area = request.Area?.Trim();
        stp.Purpose = request.Purpose;
        stp.Scope = request.Scope;
        stp.Responsibility = request.Responsibility;
        stp.Accountability = request.Accountability;
        stp.ReviewDate = request.ReviewDate;
        stp.IssueDate = request.IssueDate;
        stp.UpdatedAt = DateTime.UtcNow;
        stp.LastUpdatedById = userId;

        // A reviewer must not be evaluating a moving target: any edit while under review
        // sends the document back to Draft, so review starts over.
        if (stp.Status == QcDocumentStatus.UnderReview)
        {
            stp.Status = QcDocumentStatus.Draft;
            stp.Approved = false;
        }

        // Replace the steps through the DbSet rather than through the navigation property.
        // The context soft-deletes by rewriting a Deleted entry as Modified, which does not
        // survive EF's orphan-cascade bookkeeping; going through the set keeps both the
        // removal and the insert explicit.
        var existingSteps = await context.QcStpSteps
            .Where(step => step.StandardTestProcedureId == id)
            .ToListAsync();

        context.QcStpSteps.RemoveRange(existingSteps);

        foreach (var step in BuildSteps(request.Steps, userId))
        {
            step.StandardTestProcedureId = id;
            context.QcStpSteps.Add(step);
        }

        await context.SaveChangesAsync();
        return await GetStp(id);
    }

    public async Task<Result<StpDetailDto>> CreateNewVersion(Guid id, Guid userId)
    {
        var source = await LoadDetail(id);
        if (source is null)
            return Result.Failure<StpDetailDto>(QcWorksheetErrors.StpNotFound(id));

        if (source.Status != QcDocumentStatus.Effective)
            return Result.Failure<StpDetailDto>(
                QcWorksheetErrors.NewVersionRequiresEffective(source.Status));

        // The source record is untouched and stays in force until the new draft completes
        // its own full approval cycle.
        var draft = new StandardTestProcedure
        {
            Id = Guid.NewGuid(),
            Code = source.Code,
            Name = source.Name,
            Area = source.Area,
            Purpose = source.Purpose,
            Scope = source.Scope,
            Responsibility = source.Responsibility,
            Accountability = source.Accountability,
            ReviewDate = source.ReviewDate,
            IssueDate = source.IssueDate,
            Version = source.Version + 1,
            SupersedesId = source.Id,
            Status = QcDocumentStatus.Draft,
            Approved = false,
            CreatedAt = DateTime.UtcNow,
            CreatedById = userId,
            Steps = source.Steps
                .OrderBy(step => step.Order)
                .Select(step => new StpStep
                {
                    Id = Guid.NewGuid(),
                    Order = step.Order,
                    Title = step.Title,
                    Instruction = step.Instruction,
                    ReferencedStpId = step.ReferencedStpId,
                    CreatedAt = DateTime.UtcNow,
                    CreatedById = userId
                })
                .ToList()
        };

        context.QcStandardTestProcedures.Add(draft);
        await context.SaveChangesAsync();

        return await GetStp(draft.Id);
    }

    // -----------------------------------------------------------------------
    // Lifecycle
    // -----------------------------------------------------------------------

    public async Task<Result<StpDetailDto>> SubmitForReview(Guid id, Guid userId)
    {
        var stp = await context.QcStandardTestProcedures.SingleOrDefaultAsync(item => item.Id == id);
        if (stp is null)
            return Result.Failure<StpDetailDto>(QcWorksheetErrors.StpNotFound(id));

        if (stp.Status != QcDocumentStatus.Draft)
            return Result.Failure<StpDetailDto>(QcWorksheetErrors.SubmitRequiresDraft(stp.Status));

        // QC opts out of the engine's silent auto-approval fallback: without a configured
        // chain there is nobody to sign, so this fails rather than self-approving.
        if (!await HasConfiguredApprovalChain())
            return Result.Failure<StpDetailDto>(
                QcWorksheetErrors.NoApprovalWorkflowConfigured(ModelType));

        stp.Status = QcDocumentStatus.UnderReview;
        stp.UpdatedAt = DateTime.UtcNow;
        stp.LastUpdatedById = userId;
        await context.SaveChangesAsync();

        // The real engine creates the ResponsibleApprovalStage rows and puts this into the
        // approvers' pending queue.
        await approvalRepository.CreateInitialApprovalsAsync(ModelType, id);

        return await GetStp(id);
    }

    public async Task<Result<StpDetailDto>> Approve(
        Guid id, QcApprovalRequest request, Guid userId, List<Guid> roleIds)
    {
        var stp = await context.QcStandardTestProcedures.SingleOrDefaultAsync(item => item.Id == id);
        if (stp is null)
            return Result.Failure<StpDetailDto>(QcWorksheetErrors.StpNotFound(id));

        if (stp.Status != QcDocumentStatus.UnderReview)
            return Result.Failure<StpDetailDto>(
                QcWorksheetErrors.ApproveRequiresUnderReview(stp.Status));

        var signed = await signatureService.SignAndApproveAsync(
            ModelType, id, userId, roleIds, request.Password, request.Comments);

        return !signed.IsSuccess
            ? Result.Failure<StpDetailDto>(signed.Error)
            : await GetStp(id);
    }

    public async Task<Result<StpDetailDto>> Reject(
        Guid id, QcApprovalRequest request, Guid userId, List<Guid> roleIds)
    {
        var stp = await context.QcStandardTestProcedures.SingleOrDefaultAsync(item => item.Id == id);
        if (stp is null)
            return Result.Failure<StpDetailDto>(QcWorksheetErrors.StpNotFound(id));

        if (stp.Status != QcDocumentStatus.UnderReview)
            return Result.Failure<StpDetailDto>(
                QcWorksheetErrors.ApproveRequiresUnderReview(stp.Status));

        var signed = await signatureService.SignAndRejectAsync(
            ModelType, id, userId, roleIds, request.Password, request.Comments);

        return !signed.IsSuccess
            ? Result.Failure<StpDetailDto>(signed.Error)
            : await GetStp(id);
    }

    public async Task<Result<StpDetailDto>> MakeEffective(Guid id, Guid userId)
    {
        var stp = await context.QcStandardTestProcedures.SingleOrDefaultAsync(item => item.Id == id);
        if (stp is null)
            return Result.Failure<StpDetailDto>(QcWorksheetErrors.StpNotFound(id));

        if (stp.Status != QcDocumentStatus.Approved)
            return Result.Failure<StpDetailDto>(
                QcWorksheetErrors.MakeEffectiveRequiresApproved(stp.Status));

        stp.Status = QcDocumentStatus.Effective;
        stp.EffectiveDate = DateTime.UtcNow;
        stp.UpdatedAt = DateTime.UtcNow;
        stp.LastUpdatedById = userId;

        // The predecessor only retires now — not when this version was drafted.
        if (stp.SupersedesId.HasValue)
        {
            var superseded = await context.QcStandardTestProcedures
                .SingleOrDefaultAsync(item => item.Id == stp.SupersedesId.Value);

            if (superseded is { Status: QcDocumentStatus.Effective })
            {
                superseded.Status = QcDocumentStatus.Superseded;
                superseded.UpdatedAt = DateTime.UtcNow;
                superseded.LastUpdatedById = userId;
            }
        }

        await context.SaveChangesAsync();
        return await GetStp(id);
    }

    public async Task<Result<StpDetailDto>> Supersede(
        Guid id, QcSupersedeRequest request, Guid userId)
    {
        var stp = await context.QcStandardTestProcedures.SingleOrDefaultAsync(item => item.Id == id);
        if (stp is null)
            return Result.Failure<StpDetailDto>(QcWorksheetErrors.StpNotFound(id));

        if (stp.Status != QcDocumentStatus.Effective)
            return Result.Failure<StpDetailDto>(
                QcWorksheetErrors.SupersedeRequiresEffective(stp.Status));

        if (string.IsNullOrWhiteSpace(request.Comments))
            return Result.Failure<StpDetailDto>(QcWorksheetErrors.ReasonForChangeRequired);

        var approval = await context.Approvals.FirstOrDefaultAsync(item => item.ItemType == ModelType);
        if (approval is null)
            return Result.Failure<StpDetailDto>(
                QcWorksheetErrors.NoApprovalWorkflowConfigured(ModelType));

        var verified = await signatureService.VerifyAsync(userId, request.Password);
        if (!verified.IsSuccess)
            return Result.Failure<StpDetailDto>(verified.Error);

        var recorded = await QcApprovalHandler.RecordSignedActionAsync(
            context,
            reauthContext,
            ModelType,
            id,
            approval.Id,
            userId,
            request.Comments);

        if (!recorded.IsSuccess)
            return Result.Failure<StpDetailDto>(recorded.Error);

        stp.Status = QcDocumentStatus.Superseded;
        stp.UpdatedAt = DateTime.UtcNow;
        stp.LastUpdatedById = userId;
        await context.SaveChangesAsync();

        return await GetStp(id);
    }

    // -----------------------------------------------------------------------
    // Import
    // -----------------------------------------------------------------------

    public async Task<Result<List<StpImportResultDto>>> Import(IFormFileCollection files, Guid userId)
    {
        if (files is null || files.Count == 0)
            return Result.Failure<List<StpImportResultDto>>(
                Error.Validation("QcStp.NoFiles", "At least one .docx file is required"));

        var results = new List<StpImportResultDto>();

        foreach (var file in files)
        {
            var parsed = await importService.ParseAsync(file);

            if (!parsed.Succeeded)
            {
                results.Add(parsed);
                continue;
            }

            var stp = new StandardTestProcedure
            {
                Id = Guid.NewGuid(),
                Code = parsed.Code,
                Name = parsed.Name,
                Area = parsed.Area,
                Purpose = parsed.Purpose,
                Scope = parsed.Scope,
                Responsibility = parsed.Responsibility,
                Accountability = parsed.Accountability,
                Version = 1,
                Status = QcDocumentStatus.Draft,
                Approved = false,
                CreatedAt = DateTime.UtcNow,
                CreatedById = userId,
                Steps = parsed.Steps.Select((step, index) => new StpStep
                {
                    Id = Guid.NewGuid(),
                    Order = index + 1,
                    Title = step.Title,
                    Instruction = step.Instruction,
                    CreatedAt = DateTime.UtcNow,
                    CreatedById = userId
                }).ToList()
            };

            context.QcStandardTestProcedures.Add(stp);
            await context.SaveChangesAsync();

            results.Add(new StpImportResultDto
            {
                FileName = parsed.FileName,
                Succeeded = true,
                StandardTestProcedureId = stp.Id,
                Code = stp.Code,
                Name = stp.Name,
                FlaggedForReview = parsed.FlaggedForReview
            });
        }

        return Result.Success(results);
    }

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

    private async Task<Result> ValidateStepReferences(
        List<CreateStpStepRequest> steps, Guid? selfId = null)
    {
        var referenced = steps?
            .Where(step => step.ReferencedStpId.HasValue)
            .Select(step => step.ReferencedStpId!.Value)
            .Distinct()
            .ToList() ?? [];

        foreach (var referenceId in referenced)
        {
            if (selfId.HasValue && referenceId == selfId.Value)
                return Error.Validation(
                    "QcStp.SelfReference",
                    "A step cannot reference the standard test procedure it belongs to.");

            if (!await context.QcStandardTestProcedures.AnyAsync(item => item.Id == referenceId))
                return QcWorksheetErrors.ReferencedStpNotFound(referenceId);
        }

        return Result.Success();
    }

    private static List<StpStep> BuildSteps(List<CreateStpStepRequest> steps, Guid userId) =>
        (steps ?? [])
        .OrderBy(step => step.Order)
        .Select((step, index) => new StpStep
        {
            Id = Guid.NewGuid(),
            Order = step.Order == 0 ? index + 1 : step.Order,
            Title = step.Title,
            Instruction = step.Instruction,
            ReferencedStpId = step.ReferencedStpId,
            CreatedAt = DateTime.UtcNow,
            CreatedById = userId
        })
        .ToList();

    /// <summary>
    /// Read path. No-tracking on purpose: the context soft-deletes by turning a Deleted
    /// entry into a Modified one, so previously removed children stay in the change tracker
    /// and would otherwise be fixed up back into this graph on a read-after-write.
    /// </summary>
    private async Task<StandardTestProcedure> LoadDetail(Guid id) =>
        await context.QcStandardTestProcedures
            .AsNoTracking()
            .Include(item => item.CreatedBy)
            .Include(item => item.Steps.OrderBy(step => step.Order))
                .ThenInclude(step => step.ReferencedStp)
            .AsSplitQuery()
            .SingleOrDefaultAsync(item => item.Id == id);

    /// <summary>
    /// Mutation path: the tracked root only. Steps are replaced through their own DbSet, so
    /// the navigation is deliberately not loaded here.
    /// </summary>
    private async Task<StandardTestProcedure> LoadForUpdate(Guid id) =>
        await context.QcStandardTestProcedures
            .SingleOrDefaultAsync(item => item.Id == id);

    private StpSummaryDto ToSummaryDto(StandardTestProcedure stp) => new()
    {
        Id = stp.Id,
        Code = stp.Code,
        Name = stp.Name,
        Area = stp.Area,
        Version = stp.Version,
        Status = stp.Status,
        Approved = stp.Approved,
        EffectiveDate = stp.EffectiveDate,
        ReviewDate = stp.ReviewDate,
        IssueDate = stp.IssueDate,
        SupersedesId = stp.SupersedesId,
        CreatedAt = stp.CreatedAt,
        CreatedBy = mapper.Map<UserDto>(stp.CreatedBy)
    };

    private StpDetailDto ToDetailDto(StandardTestProcedure stp) => new()
    {
        Id = stp.Id,
        Code = stp.Code,
        Name = stp.Name,
        Area = stp.Area,
        Version = stp.Version,
        Status = stp.Status,
        Approved = stp.Approved,
        EffectiveDate = stp.EffectiveDate,
        ReviewDate = stp.ReviewDate,
        IssueDate = stp.IssueDate,
        SupersedesId = stp.SupersedesId,
        CreatedAt = stp.CreatedAt,
        CreatedBy = mapper.Map<UserDto>(stp.CreatedBy),
        Purpose = stp.Purpose,
        Scope = stp.Scope,
        Responsibility = stp.Responsibility,
        Accountability = stp.Accountability,
        Steps = stp.Steps
            .OrderBy(step => step.Order)
            .Select(step => new StpStepDto
            {
                Id = step.Id,
                Order = step.Order,
                Title = step.Title,
                Instruction = step.Instruction,
                CreatedAt = step.CreatedAt,
                // Resolved so a client renders a real link rather than plain text.
                ReferencedStp = step.ReferencedStp is null
                    ? null
                    : new StpReferenceDto
                    {
                        Id = step.ReferencedStp.Id,
                        Code = step.ReferencedStp.Code,
                        Name = step.ReferencedStp.Name,
                        Version = step.ReferencedStp.Version,
                        Status = step.ReferencedStp.Status
                    }
            })
            .ToList()
    };
}
