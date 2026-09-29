using APP.IRepository;
using APP.Services.QcWorksheets;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.QcWorksheets;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository.QcWorksheets;

/// <summary>
/// The certificate viewer, issuance and revision.
/// <para>
/// Three rules govern everything here.
/// </para>
/// <para>
/// <b>A certificate is never authored.</b> There is no create endpoint and no edit endpoint, by
/// design. A Draft appears because the round it certifies passed the strict-hold gate; the only
/// user actions are issuing it and revising it.
/// </para>
/// <para>
/// <b>Reads never re-derive.</b> Every value a certificate prints was snapshotted at generation
/// time. Nothing in this repository joins a row back to its Specification or its WorksheetInstance
/// to render it — if it did, editing a Specification would silently rewrite an issued certificate,
/// which is the exact failure this whole design exists to prevent.
/// </para>
/// <para>
/// <b>Revision supersedes, never overwrites.</b> A revision is a new record. The original stays
/// fully retrievable, is marked Superseded only once the replacement is actually issued, and is
/// never deleted or edited in place. Raising one is a meaning-of-signature event and takes the
/// same re-authentication every other QC signature does — issuing is the deliberate exception,
/// because the reviews that gated generation were each signed already, whereas withdrawing a
/// certificate already in circulation is a fresh decision by a named person.
/// </para>
/// </summary>
public class CoaRepository(
    ApplicationDbContext context,
    IMapper mapper,
    IQcSignatureService signatureService,
    IQcReauthContext reauthContext,
    IQcCoaGenerationService generationService,
    IQcWaterQualityPeriodService waterQualityPeriodService) : ICoaRepository
{
    private const string ModelType = QcWorksheetModelTypes.Coa;

    // -----------------------------------------------------------------------
    // Reads
    // -----------------------------------------------------------------------

    public async Task<Result<Paginateable<IEnumerable<CoaSummaryDto>>>> GetCoas(
        int page,
        int pageSize,
        string searchQuery,
        CoaStatus? status,
        CoaCertificateShape? shape,
        DateTime? from,
        DateTime? to)
    {
        var query = context.Coas.AsNoTracking().AsQueryable();

        if (status.HasValue)
            query = query.Where(item => item.Status == status.Value);

        if (shape.HasValue)
            query = query.Where(item => item.CertificateShape == shape.Value);

        // The date range filters on issuance, not creation: "which certificates went out in
        // September" is the question a certificate register is asked, and a Draft has no answer
        // to it, so a bounded search deliberately excludes unissued drafts.
        if (from.HasValue)
            query = query.Where(item => item.IssuedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(item => item.IssuedAt <= to.Value);

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            var term = searchQuery.Trim().ToLower();
            query = query.Where(item =>
                item.CertificateCode.ToLower().Contains(term)
                || item.ProductOrMaterialName.ToLower().Contains(term)
                || item.AreaOrRoom.ToLower().Contains(term)
                || item.BatchNumber.ToLower().Contains(term)
                || item.SpecificationCode.ToLower().Contains(term));
        }

        var ordered = query
            .Include(item => item.IssuedBy)
            .Include(item => item.TestRequest)
            .OrderByDescending(item => item.CreatedAt);

        return await PaginationHelper.GetPaginatedResultAsync(ordered, page, pageSize, ToSummaryDto);
    }

    public async Task<Result<CoaDetailDto>> GetCoa(Guid id)
    {
        var coa = await LoadDetail(id);

        return coa is null
            ? Result.Failure<CoaDetailDto>(QcWorksheetErrors.CoaNotFound(id))
            : Result.Success(await ToDetailDto(coa));
    }

    // -----------------------------------------------------------------------
    // Issue
    // -----------------------------------------------------------------------

    /// <summary>
    /// Draft to Issued.
    /// <para>
    /// Deliberately without a re-authentication wrapper, unlike every other QC transition.
    /// Issuance is not itself an Approval-chain action: the WorksheetInstance reviews that gated
    /// this certificate's generation each went through the full re-authenticated Approval flow in
    /// Milestone 3, and there is no separate authorship here for a second signature to attest to.
    /// </para>
    /// <para>
    /// Issuing is what finally closes the round. It locks the worksheets the certificate draws on
    /// — honouring the contract <c>WorksheetInstanceStatus.Locked</c> was defined against in
    /// Milestone 3 — and moves the round to Released, which Milestone 3's status calculator
    /// explicitly left to this milestone.
    /// </para>
    /// </summary>
    public async Task<Result<CoaDetailDto>> Issue(Guid id, Guid userId)
    {
        var coa = await context.Coas.SingleOrDefaultAsync(item => item.Id == id);
        if (coa is null)
            return Result.Failure<CoaDetailDto>(QcWorksheetErrors.CoaNotFound(id));

        if (coa.Status != CoaStatus.Draft)
            return Result.Failure<CoaDetailDto>(QcWorksheetErrors.IssueRequiresDraft(coa.Status));

        coa.Status = CoaStatus.Issued;
        coa.IssuedAt = DateTime.UtcNow;
        coa.IssuedById = userId;
        coa.UpdatedAt = DateTime.UtcNow;
        coa.LastUpdatedById = userId;

        // The predecessor moves to Superseded here and not at /revise: an unissued draft
        // supersedes nothing, and marking the live certificate obsolete before its replacement
        // exists would leave the round with no valid certificate at all in between.
        if (coa.SupersedesId.HasValue)
        {
            var superseded = await context.Coas
                .SingleOrDefaultAsync(item => item.Id == coa.SupersedesId.Value);

            if (superseded is not null && superseded.Status == CoaStatus.Issued)
            {
                superseded.Status = CoaStatus.Superseded;
                superseded.UpdatedAt = DateTime.UtcNow;
                superseded.LastUpdatedById = userId;
            }
        }

        await context.SaveChangesAsync();

        await LockCertifiedWorksheets(coa.TestRequestId, userId);
        await ReleaseRound(coa.TestRequestId, userId);

        // Milestone 6. A Water certificate scaffolds a validity window per Subject, at
        // PendingActivation — it covers nothing until somebody activates it with a stated reason.
        // A no-op for every other round type, which is why it is called unconditionally rather
        // than behind a type check duplicated here.
        await waterQualityPeriodService.ScaffoldForIssuedCoaAsync(coa.TestRequestId, userId);

        return await GetCoa(id);
    }

    /// <summary>
    /// Freezes every worksheet the issued certificate draws on.
    /// <para>
    /// <c>Locked</c> is honoured as terminal everywhere it is read — no reassignment, no edit, no
    /// review — which is what makes an issued certificate's underlying record immutable in the
    /// system and not merely in the certificate's own snapshot. A later retest is a <i>new</i>
    /// instance and is unaffected, which is what leaves the revision path open.
    /// </para>
    /// </summary>
    private async Task LockCertifiedWorksheets(Guid testRequestId, Guid userId)
    {
        var instances = await context.QcWorksheetInstances
            .Where(item => item.Status == WorksheetInstanceStatus.Reviewed
                && context.QcTestRequestSubjects.Any(subject =>
                    subject.Id == item.TestRequestSubjectId
                    && subject.TestRequestId == testRequestId))
            .ToListAsync();

        if (instances.Count == 0)
            return;

        foreach (var instance in instances)
        {
            instance.Status = WorksheetInstanceStatus.Locked;
            instance.UpdatedAt = DateTime.UtcNow;
            instance.LastUpdatedById = userId;
        }

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Moves the round to Released.
    /// <para>
    /// <c>QcTestRequestStatusCalculator</c> deliberately refuses to write this: Released depends
    /// on certificate issue, which is this milestone's, so the round's terminal state is set here
    /// and nowhere else.
    /// </para>
    /// </summary>
    private async Task ReleaseRound(Guid testRequestId, Guid userId)
    {
        var round = await context.QcTestRequests.SingleOrDefaultAsync(item => item.Id == testRequestId);

        if (round is null || round.Status == TestRequestStatus.Released)
            return;

        round.Status = TestRequestStatus.Released;
        round.UpdatedAt = DateTime.UtcNow;
        round.LastUpdatedById = userId;

        await context.SaveChangesAsync();
    }

    // -----------------------------------------------------------------------
    // Revise
    // -----------------------------------------------------------------------

    /// <summary>
    /// Reissues a certificate as a new record, recomputing its rows from current data — a retest
    /// completed, or a data-entry correction was made.
    /// <para>
    /// Requires a re-authenticated signature. Withdrawing a certificate that is already in
    /// circulation is a decision a named person makes and answers for, so it takes the acting
    /// user's own password on top of a valid session, recorded in the same shared
    /// <see cref="QcApproval"/> table as every other QC signature — against the <i>original</i>,
    /// so its trail answers "who authorized withdrawing this, and when".
    /// </para>
    /// <para>
    /// The original is left exactly as it is. It only becomes Superseded when this new draft is
    /// itself issued, and it is never edited or deleted at any point. Every check and the
    /// signature itself happen before a single write, so a refused or failed signature leaves the
    /// original untouched rather than needing to be rolled back.
    /// </para>
    /// </summary>
    public async Task<Result<CoaDetailDto>> Revise(Guid id, ReviseCoaRequest request, Guid userId)
    {
        var original = await context.Coas.SingleOrDefaultAsync(item => item.Id == id);
        if (original is null)
            return Result.Failure<CoaDetailDto>(QcWorksheetErrors.CoaNotFound(id));

        if (original.Status != CoaStatus.Issued)
            return Result.Failure<CoaDetailDto>(QcWorksheetErrors.ReviseRequiresIssued(original.Status));

        if (string.IsNullOrWhiteSpace(request?.Reason))
            return Result.Failure<CoaDetailDto>(QcWorksheetErrors.RevisionReasonRequired);

        // One outstanding revision at a time. Two drafts both claiming to replace the same
        // certificate would race to supersede it, and whichever was issued second would silently
        // orphan the other.
        var pending = await context.Coas
            .AsNoTracking()
            .AnyAsync(item => item.SupersedesId == id && item.Status == CoaStatus.Draft);

        if (pending)
            return Result.Failure<CoaDetailDto>(QcWorksheetErrors.RevisionAlreadyInProgress);

        // A revision recomputes from live data, so the round has to still satisfy the same gate
        // the original passed. A round that has fallen back out of it — a fresh OOS case opened
        // against a retest, say — must not be able to emit a certificate through the side door.
        var hold = await generationService.EvaluateHoldAsync(original.TestRequestId);
        if (!hold.Satisfied)
            return Result.Failure<CoaDetailDto>(QcWorksheetErrors.CoaGenerationHeld(hold.Reason));

        var approval = await context.Approvals.FirstOrDefaultAsync(item => item.ItemType == ModelType);
        var configured = approval is not null && await context.ApprovalStages
            .AnyAsync(stage => stage.ApprovalId == approval.Id);

        // Built before the signature is taken but deliberately not persisted yet: the signer is
        // signing for a document that exists, and a signature is never recorded against a
        // revision that then failed to assemble.
        var revision = await generationService.BuildAsync(original.TestRequestId, userId);
        if (revision is null)
            return Result.Failure<CoaDetailDto>(QcWorksheetErrors.CoaGenerationHeld(
                "The round's data could not be assembled into a certificate."));

        if (configured)
        {
            var verified = await signatureService.VerifyAsync(userId, request.Password);
            if (!verified.IsSuccess)
                return Result.Failure<CoaDetailDto>(verified.Error);
            var recorded = await QcApprovalHandler.RecordSignedActionAsync(
                context, reauthContext, ModelType, original.Id,
                approval!.Id, userId, request.Reason.Trim());
            if (!recorded.IsSuccess)
                return Result.Failure<CoaDetailDto>(recorded.Error);
        }
        else
        {
            context.ApprovalActionLogs.Add(new ApprovalActionLog
            {
                ModelId = original.Id,
                UserId = null,
                Status = ApprovalStatus.Approved,
                Comments = $"SystemAutoApproved COA revision; triggered by {userId}. " +
                    request.Reason.Trim(),
            });
        }

        revision.SupersedesId = original.Id;
        revision.RevisionNumber = original.RevisionNumber + 1;
        revision.RevisionReason = request.Reason.Trim();

        // A revision carries its own code derived from the original's, not a new number from the
        // series: both documents exist at once, and a reader must be able to tell at a glance
        // that one replaces the other.
        revision.CertificateCode = $"{RootCode(original.CertificateCode)}-R{revision.RevisionNumber}";

        context.Coas.Add(revision);
        await context.SaveChangesAsync();

        return await GetCoa(revision.Id);
    }

    /// <summary>
    /// The original's code with any existing revision suffix removed, so a third revision reads
    /// <c>COA-2026-00842-R3</c> rather than <c>COA-2026-00842-R2-R3</c>.
    /// </summary>
    private static string RootCode(string certificateCode)
    {
        if (string.IsNullOrWhiteSpace(certificateCode))
            return certificateCode;

        var marker = certificateCode.LastIndexOf("-R", StringComparison.Ordinal);
        if (marker <= 0)
            return certificateCode;

        var suffix = certificateCode[(marker + 2)..];

        return suffix.Length > 0 && suffix.All(char.IsDigit)
            ? certificateCode[..marker]
            : certificateCode;
    }

    // -----------------------------------------------------------------------
    // Projection
    // -----------------------------------------------------------------------

    private async Task<Coa> LoadDetail(Guid id) =>
        await context.Coas
            .AsNoTracking()
            .Include(item => item.IssuedBy)
            .Include(item => item.TestRequest)
            .Include(item => item.Supersedes)
            .Include(item => item.Rows)
            .AsSplitQuery()
            .SingleOrDefaultAsync(item => item.Id == id);

    private CoaSummaryDto ToSummaryDto(Coa coa) => Populate(new CoaSummaryDto(), coa);

    private async Task<CoaDetailDto> ToDetailDto(Coa coa)
    {
        var dto = Populate(new CoaDetailDto(), coa);

        dto.RevisionReason = coa.RevisionReason;
        dto.SupersedesCertificateCode = coa.Supersedes?.CertificateCode;
        dto.Header = BuildHeader(coa);
        dto.Subjects = BuildSubjectGroups(coa);

        // The far side of the link: a superseded certificate has to point its reader forward to
        // whatever replaced it, or the "SUPERSEDED" banner is a dead end.
        var successor = await context.Coas
            .AsNoTracking()
            .Where(item => item.SupersedesId == coa.Id && item.Status != CoaStatus.Draft)
            .Select(item => new { item.Id, item.CertificateCode })
            .FirstOrDefaultAsync();

        if (successor is not null)
        {
            dto.SupersededById = successor.Id;
            dto.SupersededByCertificateCode = successor.CertificateCode;
        }

        return dto;
    }

    /// <summary>
    /// Fills the summary fields on either a summary or a detail DTO, so the list row and the
    /// header of the detail view can never drift apart.
    /// </summary>
    private T Populate<T>(T target, Coa coa) where T : CoaSummaryDto
    {
        target.Id = coa.Id;
        target.CreatedAt = coa.CreatedAt;
        target.TestRequestId = coa.TestRequestId;
        target.CertificateCode = coa.CertificateCode;
        target.CertificateShape = coa.CertificateShape;
        target.Status = coa.Status;
        target.RevisionNumber = coa.RevisionNumber;
        target.IssuedAt = coa.IssuedAt;
        target.IssuedBy = coa.IssuedBy is null ? null : mapper.Map<UserDto>(coa.IssuedBy);

        target.Subject = coa.CertificateShape == CoaCertificateShape.EnvironmentalMonitoringReport
            ? coa.AreaOrRoom
            : coa.ProductOrMaterialName;

        target.ArNumber = coa.TestRequest?.ArNumber;
        target.SpecificationCode = coa.SpecificationCode;
        target.SpecificationVersion = coa.SpecificationVersion;
        target.OverallComplies = coa.OverallComplies;
        target.SupersedesId = coa.SupersedesId;

        return target;
    }

    /// <summary>
    /// Renders the shape's own header. The Environmental type genuinely does not declare a
    /// Manufacturing or Expiry Date property, so the report cannot print a blank one.
    /// </summary>
    private static CoaHeaderDto BuildHeader(Coa coa)
    {
        if (coa.CertificateShape == CoaCertificateShape.EnvironmentalMonitoringReport)
        {
            return Common(new EnvironmentalMonitoringReportHeaderDto { AreaOrRoom = coa.AreaOrRoom }, coa);
        }

        return Common(
            new CertificateOfAnalysisHeaderDto
            {
                ProductOrMaterialName = coa.ProductOrMaterialName,
                BatchNumber = coa.BatchNumber,
                ManufacturingDate = coa.ManufacturingDate,
                ExpiryDate = coa.ExpiryDate
            },
            coa);

        static T Common<T>(T header, Coa coa) where T : CoaHeaderDto
        {
            header.CertificateCode = coa.CertificateCode;
            header.SpecificationCode = coa.SpecificationCode;
            header.SpecificationVersion = coa.SpecificationVersion;
            header.SpecificationRevision = $"Rev {coa.SpecificationVersion}";
            header.SampleDate = coa.SampleDate;
            header.TestCompletionDate = coa.TestCompletionDate;
            return header;
        }
    }

    /// <summary>
    /// Groups the snapshotted rows as the document prints them: one section per Subject — one per
    /// room on a Monitoring Report, matching the real per-room layout — then by GroupName, each
    /// group in DisplayOrder.
    /// </summary>
    private static List<CoaSubjectGroupDto> BuildSubjectGroups(Coa coa)
    {
        return coa.Rows
            .GroupBy(row => row.TestRequestSubjectId)

            // Ordered by the Subject's own code rather than by row order: the rows come back from
            // the database unordered, and a certificate that listed its rooms differently on two
            // consecutive views would be a certificate nobody could check against a printout.
            .OrderBy(subject => subject.First().SubjectRef, StringComparer.Ordinal)
            .Select(subject => new CoaSubjectGroupDto
            {
                TestRequestSubjectId = subject.Key,
                SubjectRef = subject.First().SubjectRef,
                SubjectLabel = subject.First().SubjectLabel,
                // Consistent with the certificate's overall verdict: a resolved finding does not
                // make a section read as failing.
                Complies = !subject.Any(CoaRowVerdict.IsUnresolvedFailure),
                Groups = subject
                    .GroupBy(row => row.GroupName ?? string.Empty)
                    .OrderBy(group => group.Key, StringComparer.Ordinal)
                    .Select(group => new CoaRowGroupDto
                    {
                        GroupName = string.IsNullOrEmpty(group.Key) ? null : group.Key,
                        Rows = group
                            .OrderBy(row => row.DisplayOrder)
                            .ThenBy(row => row.DisplayLabel, StringComparer.Ordinal)
                            .Select(ToRowDto)
                            .ToList()
                    })
                    .ToList()
            })
            .ToList();
    }

    private static CoaRowDto ToRowDto(CoaRow row) => new()
    {
        Id = row.Id,
        TestRequestSubjectId = row.TestRequestSubjectId,
        SpecificationCharacteristicId = row.SpecificationCharacteristicId,
        SourceWorksheetInstanceId = row.SourceWorksheetInstanceId,
        DisplayLabel = row.DisplayLabel,
        GroupName = row.GroupName,
        DisplayOrder = row.DisplayOrder,
        AcceptanceCriteria = row.AcceptanceCriteria,

        // The measured value always renders. A disposition annotates it; it never erases it.
        ResultValue = row.ResultValue,
        Complies = row.Complies,

        DispositionOutcome = row.DispositionOutcome,
        DispositionReason = row.DispositionReason,

        // Computed from the snapshot rather than stored, so the wording of a label can be improved
        // later without rewriting certificates that were already issued — the facts it is built
        // from are the frozen part.
        ComplianceLabel = CoaRowVerdict.Label(row),
        IsUnresolvedFailure = CoaRowVerdict.IsUnresolvedFailure(row)
    };
}
