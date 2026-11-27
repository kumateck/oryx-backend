using APP.IRepository;
using APP.Services.Authorization;
using APP.Services.Reviews;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Reviews;
using DOMAIN.Entities.StpRevisions;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Services.StpRevisions;

/// <summary>
/// Structured STP revisions, on the approved vendor list's pattern: the first revision of an STP number, drafts,
/// submission to the document review ("Checked by") and the approval engine ("Approved by"), corrections while in review,
/// new revisions of the approved one, and deletion with a reason. Import reads a Word STP into a proposal and saves nothing.
/// </summary>
public sealed partial class StpRevisionService(
    ApplicationDbContext context,
    IApprovalRepository approvals,
    APP.Services.QcWorksheets.IStpDocxImportService importer,
    IStpDocumentRepository documents)
{
    public const string ModelType = ReviewDocumentTypes.StpRevision;

    /// <summary>Every revision of a kind, or of one STP number, newest first.</summary>
    public async Task<Result<List<StpRevisionSummaryDto>>> List(StpRevisionKind kind, string stpNumber, Guid actorId)
    {
        var refused = await Refuse(StpRevisionTask.View, kind, actorId);
        if (refused is not null) return refused;
        var number = stpNumber?.Trim();
        var rows = await Summaries(context.StpRevisions.AsNoTracking()
            .Where(item => item.Kind == kind && (number == null || number == "" || item.StpNumber == number)));
        await AttachReviewStatus(rows);
        return rows;
    }

    /// <summary>The STP numbers of a kind (from the material or product STP records), each with its current and in-preparation revision.</summary>
    public async Task<Result<List<StpNumberOverviewDto>>> Numbers(StpRevisionKind kind, Guid actorId)
    {
        var refused = await Refuse(StpRevisionTask.View, kind, actorId);
        if (refused is not null) return refused;
        var numbers = await StpNumbers(kind);
        var revisions = await Summaries(context.StpRevisions.AsNoTracking().Where(item => item.Kind == kind));
        await AttachReviewStatus(revisions);
        return numbers.Union(revisions.Select(item => item.StpNumber), StringComparer.OrdinalIgnoreCase)
            .OrderBy(item => item, StringComparer.OrdinalIgnoreCase)
            .Select(number => new StpNumberOverviewDto
            {
                StpNumber = number,
                Current = revisions.FirstOrDefault(item => Same(item.StpNumber, number) && item.Status == StpRevisionStatus.Approved),
                InPreparation = revisions.FirstOrDefault(item => Same(item.StpNumber, number)
                    && item.Status is StpRevisionStatus.Draft or StpRevisionStatus.Pending),
                RevisionCount = revisions.Count(item => Same(item.StpNumber, number)),
            }).ToList();
    }

    public async Task<Result<StpRevisionDto>> Get(Guid id, Guid actorId)
    {
        var revision = await context.StpRevisions.AsNoTracking()
            .Include(item => item.Sections)
            .Include(item => item.Tests).ThenInclude(test => test.Steps)
            .Include(item => item.CreatedBy)
            .AsSplitQuery()
            .FirstOrDefaultAsync(item => item.Id == id);
        if (revision is null) return NotFound;
        var refused = await Refuse(StpRevisionTask.View, revision.Kind, actorId);
        if (refused is not null) return refused;
        var review = await DocumentReviewGate.LatestAsync(context, ModelType, id);
        var confirmed = await context.DocumentReviews.AsNoTracking()
            .Where(item => item.ModelType == ModelType && item.ModelId == id && item.Status == DocumentReviewStatus.Confirmed)
            .OrderByDescending(item => item.Submission).FirstOrDefaultAsync();
        var approver = await ReviewDocumentTypes.FinalApproverAsync(context, ModelType, id);
        var people = await Signatories([revision.CreatedById, confirmed?.ReviewedById, approver.UserId]);
        return new StpRevisionDto
        {
            Id = revision.Id, Kind = revision.Kind, StpNumber = revision.StpNumber, RevisionNumber = revision.RevisionNumber,
            SupersedesRevision = revision.SupersedesRevision, Status = revision.Status, Title = revision.Title,
            TestCount = revision.Tests.Count, CreatedAt = revision.CreatedAt, ApprovedAt = revision.ApprovedAt,
            SupersededAt = revision.SupersededAt, RevisedFromId = revision.RevisedFromId, ReasonForChange = revision.ReasonForChange,
            Area = revision.Area, Reference = revision.Reference, EffectiveDate = revision.EffectiveDate, ReviewDate = revision.ReviewDate,
            IssueDate = revision.IssueDate, Source = revision.Source, Version = revision.Version,
            PreparedBy = revision.CreatedBy == null ? null : $"{revision.CreatedBy.FirstName} {revision.CreatedBy.LastName}".Trim(),
            ReviewStatus = revision.Status == StpRevisionStatus.Pending ? OpenReviewStatus(review) : null,
            ReviewVersion = review?.Version,
            PreparedBySignatory = Sign(people, revision.CreatedById, revision.CreatedAt),
            CheckedBy = Sign(people, confirmed?.ReviewedById, confirmed?.ReviewedAt),
            ApprovedBy = Sign(people, approver.UserId, approver.At),
            Sections = revision.Sections.OrderBy(item => item.Order)
                .Select(item => new StpSectionDto { Id = item.Id, Kind = item.Kind, Order = item.Order, Text = item.Text }).ToList(),
            Tests = revision.Tests.OrderBy(item => item.Order).Select(test => new StpTestDto
            {
                Id = test.Id, TestKey = test.TestKey, Number = test.Number, Title = test.Title, Order = test.Order,
                Steps = test.Steps.OrderBy(step => step.Order).Select(step => new StpStepDto
                {
                    Id = step.Id, Order = step.Order, Kind = step.Kind, Number = step.Number, Heading = step.Heading,
                    Text = step.Text, ReferenceCode = step.ReferenceCode,
                }).ToList(),
            }).ToList(),
        };
    }

    private static Task<List<StpRevisionSummaryDto>> Summaries(IQueryable<StpRevision> query) => query
        .OrderBy(item => item.StpNumber).ThenByDescending(item => item.RevisionNumber).ThenByDescending(item => item.CreatedAt)
        .Select(item => new StpRevisionSummaryDto
        {
            Id = item.Id, Kind = item.Kind, StpNumber = item.StpNumber, RevisionNumber = item.RevisionNumber,
            SupersedesRevision = item.SupersedesRevision, Status = item.Status, Title = item.Title, TestCount = item.Tests.Count,
            CreatedAt = item.CreatedAt, ApprovedAt = item.ApprovedAt, SupersededAt = item.SupersededAt,
            PreparedBy = item.CreatedBy == null ? null : (item.CreatedBy.FirstName + " " + item.CreatedBy.LastName).Trim(),
        }).ToListAsync();

    private async Task AttachReviewStatus(List<StpRevisionSummaryDto> rows)
    {
        var pending = rows.Where(item => item.Status == StpRevisionStatus.Pending).Select(item => item.Id).ToList();
        if (pending.Count == 0) return;
        var reviews = await context.DocumentReviews.AsNoTracking()
            .Where(item => item.ModelType == ModelType && pending.Contains(item.ModelId))
            .ToListAsync();
        foreach (var row in rows.Where(item => item.Status == StpRevisionStatus.Pending))
            row.ReviewStatus = OpenReviewStatus(reviews.Where(review => review.ModelId == row.Id).MaxBy(review => review.Submission));
    }

    /// <summary>STP numbers recorded on material STPs of that kind, or on product STPs.</summary>
    private async Task<List<string>> StpNumbers(StpRevisionKind kind)
    {
        List<string> numbers = kind == StpRevisionKind.Product
            ? await context.ProductStandardTestProcedures.AsNoTracking().Select(item => item.StpNumber).ToListAsync()
            : await context.MaterialStandardTestProcedures.AsNoTracking()
                .Where(item => item.Material.Kind == (kind == StpRevisionKind.RawMaterial ? MaterialKind.Raw : MaterialKind.Package))
                .Select(item => item.StpNumber).ToListAsync();
        return numbers.Where(item => !string.IsNullOrWhiteSpace(item)).Select(item => item.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static bool Same(string a, string b) => string.Equals(a?.Trim(), b?.Trim(), StringComparison.OrdinalIgnoreCase);

    private static Error NotFound => Error.NotFound("StpRevision.NotFound", "STP revision not found.");

    private static DocumentReviewStatus? OpenReviewStatus(DocumentReview review) =>
        review is { Status: DocumentReviewStatus.AwaitingReview or DocumentReviewStatus.Returned } ? review.Status : null;

    /// <summary>Null when the actor holds the task's key for the kind; otherwise the refusal.</summary>
    private async Task<Error?> Refuse(StpRevisionTask task, StpRevisionKind kind, Guid actorId)
    {
        if (!Enum.IsDefined(kind)) return Error.Validation("StpRevision.Kind", "Choose raw material, packaging material or product.");
        var key = StpRevisionPermissions.Key(task, kind);
        return (await CurrentPermissionSet.Load(context, actorId)).Contains(key) ? null
            : Error.Forbidden("StpRevision.Permission", $"You need {key} for this.");
    }

    private async Task<Dictionary<Guid, StpRevisionSignatoryDto>> Signatories(IEnumerable<Guid?> ids)
    {
        var wanted = ids.Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
        var users = await context.Users.IgnoreQueryFilters().AsNoTracking().Where(user => wanted.Contains(user.Id))
            .Select(user => new { user.Id, Name = (user.FirstName + " " + user.LastName).Trim(), user.Signature })
            .ToListAsync();
        var designations = await context.Employees.IgnoreQueryFilters().AsNoTracking()
            .Where(employee => employee.UserId.HasValue && wanted.Contains(employee.UserId.Value) && employee.Designation != null)
            .Select(employee => new { UserId = employee.UserId!.Value, employee.Designation.Name })
            .ToListAsync();
        return users.ToDictionary(user => user.Id, user => new StpRevisionSignatoryDto
        {
            UserId = user.Id, Name = user.Name, Signature = user.Signature,
            Designation = designations.FirstOrDefault(item => item.UserId == user.Id)?.Name,
        });
    }

    private static StpRevisionSignatoryDto Sign(Dictionary<Guid, StpRevisionSignatoryDto> people, Guid? id, DateTime? at)
    {
        if (!id.HasValue || !people.TryGetValue(id.Value, out var person)) return null;
        return new StpRevisionSignatoryDto
            { UserId = person.UserId, Name = person.Name, Designation = person.Designation, Signature = person.Signature, SignedAt = at };
    }
}
