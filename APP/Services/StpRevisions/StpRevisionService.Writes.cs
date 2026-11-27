using System.Text.RegularExpressions;
using APP.Services.Authorization;
using APP.Services.QcWorksheets;
using APP.Services.Reviews;
using APP.Services.Transactions;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Reviews;
using DOMAIN.Entities.StpRevisions;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Services.StpRevisions;

public sealed partial class StpRevisionService
{
    private const int MaxTests = 200;
    private const int MaxSteps = 200;
    private const int MaxText = 20000;

    /// <summary>A revision of the STP number being prepared, as a draft or in review/approval. Only one at a time.</summary>
    private Task<bool> InPreparation(StpRevisionKind kind, string stpNumber) => context.StpRevisions.AnyAsync(item =>
        item.Kind == kind && item.StpNumber == stpNumber
        && (item.Status == StpRevisionStatus.Draft || item.Status == StpRevisionStatus.Pending));

    private Task<StpRevision> Current(StpRevisionKind kind, string stpNumber) => context.StpRevisions.FirstOrDefaultAsync(item =>
        item.Kind == kind && item.StpNumber == stpNumber && item.Status == StpRevisionStatus.Approved);

    /// <summary>Starts the first revision of an STP number as a draft. Once one is approved, changes start as a new revision.</summary>
    public Task<Result<Guid>> Create(CreateStpRevisionRequest request, Guid actorId) =>
        ReferenceMaintenanceTransaction.Run<Guid>(context, async () =>
    {
        var refused = await Refuse(StpRevisionTask.Create, request.Kind, actorId);
        if (refused is not null) return refused;
        var number = request.StpNumber?.Trim();
        if (string.IsNullOrEmpty(number)) return Error.Validation("StpRevision.Number", "Enter the STP number.");
        if (!(await StpNumbers(request.Kind)).Contains(number, StringComparer.OrdinalIgnoreCase))
            return Error.Validation("StpRevision.Number", $"{number} is not an STP number of this kind. Create the STP first.");
        number = (await StpNumbers(request.Kind)).First(item => Same(item, number));
        if (await InPreparation(request.Kind, number))
            return Error.Conflict("StpRevision.Pending", "A revision of this STP is already being prepared. Continue, submit or delete it first.");
        if (await Current(request.Kind, number) is not null)
            return Error.Conflict("StpRevision.Exists", "This STP has an approved revision already. Create a new revision of it instead.");
        if (request.SupersedesRevision is { } supersedes && supersedes >= request.RevisionNumber)
            return Error.Validation("StpRevision.Revision", "The revision number must be higher than the revision it supersedes.");
        var content = BuildContent(request, [], actorId);
        if (content.IsFailure) return content.Errors.First();
        var revision = new StpRevision
        {
            Id = Guid.NewGuid(), Kind = request.Kind, StpNumber = number, Status = StpRevisionStatus.Draft,
            CreatedById = actorId, CreatedAt = DateTime.UtcNow,
        };
        ApplyHeader(revision, request, firstRevision: true);
        revision.Sections = content.Value.Sections;
        revision.Tests = content.Value.Tests;
        refused = await Refuse(StpRevisionTask.Create, request.Kind, actorId);
        if (refused is not null) return refused;
        context.StpRevisions.Add(revision);
        await context.SaveChangesAsync();
        return revision.Id;
    });

    /// <summary>
    /// Starts a new revision of the STP number's approved revision as a draft, with a copy of its content and test keys.
    /// The approved revision stays current until the new one is approved.
    /// </summary>
    public Task<Result<Guid>> Revise(ReviseStpRevisionRequest request, Guid actorId) =>
        ReferenceMaintenanceTransaction.Run<Guid>(context, async () =>
    {
        var refused = await Refuse(StpRevisionTask.Revise, request.Kind, actorId);
        if (refused is not null) return refused;
        var number = request.StpNumber?.Trim();
        var current = await context.StpRevisions.Include(item => item.Sections).Include(item => item.Tests).ThenInclude(test => test.Steps)
            .AsSplitQuery()
            .FirstOrDefaultAsync(item => item.Kind == request.Kind && item.StpNumber == number && item.Status == StpRevisionStatus.Approved);
        if (current is null) return Error.Conflict("StpRevision.NoCurrent", "This STP has no approved revision to revise.");
        if (await InPreparation(request.Kind, current.StpNumber))
            return Error.Conflict("StpRevision.Pending", "A revision of this STP is already being prepared. Continue, submit or delete it first.");
        if (request.RevisionNumber <= current.RevisionNumber)
            return Error.Validation("StpRevision.Revision", $"The revision number must be higher than {current.RevisionNumber}.");
        var reason = request.ReasonForChange?.Trim();
        if (string.IsNullOrEmpty(reason)) return Error.Validation("StpRevision.Reason", "Enter the reason for the change.");
        var now = DateTime.UtcNow;
        var revision = new StpRevision
        {
            Id = Guid.NewGuid(), Kind = current.Kind, StpNumber = current.StpNumber, RevisionNumber = request.RevisionNumber,
            SupersedesRevision = current.RevisionNumber, RevisedFromId = current.Id, Status = StpRevisionStatus.Draft,
            ReasonForChange = reason, Title = current.Title, Area = current.Area, Reference = current.Reference,
            Source = $"Revision {current.RevisionNumber}", CreatedById = actorId, CreatedAt = now,
            Sections = current.Sections.OrderBy(item => item.Order).Select(item => new StpRevisionSection
                { Id = Guid.NewGuid(), Kind = item.Kind, Order = item.Order, Text = item.Text, CreatedById = actorId, CreatedAt = now }).ToList(),
            Tests = current.Tests.OrderBy(item => item.Order).Select(test => new StpRevisionTest
            {
                Id = Guid.NewGuid(), TestKey = test.TestKey, Number = test.Number, Title = test.Title, Order = test.Order,
                CreatedById = actorId, CreatedAt = now,
                Steps = test.Steps.OrderBy(step => step.Order).Select(step => new StpRevisionTestStep
                {
                    Id = Guid.NewGuid(), Order = step.Order, Kind = step.Kind, Number = step.Number, Heading = step.Heading,
                    Text = step.Text, ReferenceCode = step.ReferenceCode, CreatedById = actorId, CreatedAt = now,
                }).ToList(),
            }).ToList(),
        };
        refused = await Refuse(StpRevisionTask.Revise, request.Kind, actorId);
        if (refused is not null) return refused;
        context.StpRevisions.Add(revision);
        await context.SaveChangesAsync();
        return revision.Id;
    });

    /// <summary>Saves a draft's header and content, as often as needed.</summary>
    public Task<Result> SaveDraft(Guid id, SaveStpRevisionDraftRequest request, Guid actorId) =>
        ReferenceMaintenanceTransaction.Run(context, async () =>
    {
        var revision = await Load(id);
        if (revision is null) return NotFound;
        var refused = await Refuse(StpRevisionTask.EditDraft, revision.Kind, actorId);
        if (refused is not null) return refused;
        var state = DraftState(revision, request.Version);
        if (state is not null) return state;
        var header = await CheckHeader(revision, request.RevisionNumber, request.SupersedesRevision);
        if (header is not null) return header;
        var content = BuildContent(request, await KnownKeys(revision), actorId);
        if (content.IsFailure) return content.Errors.First();
        ApplyHeader(revision, request, firstRevision: !revision.RevisedFromId.HasValue);
        return await Replace(revision, content.Value, actorId, StpRevisionTask.EditDraft);
    });

    /// <summary>Submits a draft: it becomes the fixed version and goes for review, then approval.</summary>
    public Task<Result> Submit(Guid id, SubmitStpRevisionRequest request, Guid actorId) =>
        ReferenceMaintenanceTransaction.Run(context, async () =>
    {
        var revision = await Load(id);
        if (revision is null) return NotFound;
        var refused = await Refuse(StpRevisionTask.Submit, revision.Kind, actorId);
        if (refused is not null) return refused;
        var state = DraftState(revision, request?.Version);
        if (state is not null) return state;
        var complete = Complete(revision);
        if (complete is not null) return complete;
        var header = await CheckHeader(revision, revision.RevisionNumber, revision.SupersedesRevision);
        if (header is not null) return header;
        if (!revision.RevisedFromId.HasValue && await Current(revision.Kind, revision.StpNumber) is not null)
            return Error.Conflict("StpRevision.Exists", "A revision of this STP was approved since this draft started. Delete it and revise the approved one.");
        revision.Status = StpRevisionStatus.Pending;
        revision.Version++;
        revision.UpdatedAt = DateTime.UtcNow;
        revision.LastUpdatedById = actorId;
        refused = await Refuse(StpRevisionTask.Submit, revision.Kind, actorId);
        if (refused is not null) return refused;
        await context.SaveChangesAsync();
        // The review opens here (an STP is always checked); approvals start when it is confirmed.
        await approvals.CreateInitialApprovalsAsync(ModelType, revision.Id, actorId);
        return Result.Success();
    });

    /// <summary>Corrects a revision while it is in review (awaiting review or returned). The edit is recorded on the review.</summary>
    public Task<Result> Update(Guid id, UpdateStpRevisionRequest request, Guid actorId) =>
        ReferenceMaintenanceTransaction.Run(context, async () =>
    {
        var revision = await Load(id);
        if (revision is null) return NotFound;
        var refused = await Refuse(StpRevisionTask.Edit, revision.Kind, actorId);
        if (refused is not null) return refused;
        var review = await DocumentReviewGate.LatestAsync(context, ModelType, id);
        if (revision.Status != StpRevisionStatus.Pending
            || review is not { Status: DocumentReviewStatus.AwaitingReview or DocumentReviewStatus.Returned })
            return Error.Conflict("StpRevision.NotInReview", "Only a revision in review can be corrected. Revise the approved revision instead.");
        if (request.Version != revision.Version)
            return Error.Conflict("StpRevision.Stale", "The revision changed since you opened it. Reload it and try again.");
        var header = await CheckHeader(revision, request.RevisionNumber, request.SupersedesRevision);
        if (header is not null) return header;
        var content = BuildContent(request, await KnownKeys(revision), actorId);
        if (content.IsFailure) return content.Errors.First();
        ApplyHeader(revision, request, firstRevision: !revision.RevisedFromId.HasValue);
        var complete = Complete(revision, content.Value);
        if (complete is not null) return complete;
        var saved = await Replace(revision, content.Value, actorId, StpRevisionTask.Edit);
        if (saved.IsFailure) return saved;
        await DocumentReviewGate.NoteEditAsync(context, ModelType, id, actorId);
        return Result.Success();
    });

    /// <summary>Removes the revision from use while retaining its content, review and approval history.</summary>
    public Task<Result> Delete(Guid id, Guid actorId, string reason) => ReferenceMaintenanceTransaction.Run(context, async () =>
    {
        var revision = await context.StpRevisions.SingleOrDefaultAsync(item => item.Id == id);
        if (revision is null) return NotFound;
        var refused = await Refuse(StpRevisionTask.Delete, revision.Kind, actorId);
        if (refused is not null) return refused;
        var why = reason?.Trim();
        if (revision.Status == StpRevisionStatus.Draft && string.IsNullOrEmpty(why))
            return Error.Validation("StpRevision.Reason", "Enter a reason for deleting the draft.");
        if (why?.Length > 1000) return Error.Validation("StpRevision.Reason", "Keep the reason under 1000 characters.");
        refused = await Refuse(StpRevisionTask.Delete, revision.Kind, actorId);
        if (refused is not null) return refused;
        revision.DeletedAt = DateTime.UtcNow;
        revision.LastDeletedById = actorId;
        revision.DeletionReason = why;
        await context.SaveChangesAsync();
        return Result.Success();
    });

    // ---- helpers --------------------------------------------------------------------------------------------

    private Task<StpRevision> Load(Guid id) => context.StpRevisions
        .Include(item => item.Sections).Include(item => item.Tests).ThenInclude(test => test.Steps)
        .AsSplitQuery().FirstOrDefaultAsync(item => item.Id == id);

    private static Error? DraftState(StpRevision revision, int? version)
    {
        if (revision.Status != StpRevisionStatus.Draft)
            return Error.Conflict("StpRevision.NotDraft", "This revision has been submitted. Correct it through its review instead.");
        if (version != revision.Version)
            return Error.Conflict("StpRevision.Stale", "The draft was saved by someone else since you opened it. Reload it and try again.");
        return null;
    }

    /// <summary>What a submitted revision must have: a title, sections 1.0 to 4.0, and at least one test with a step.</summary>
    private static Error? Complete(StpRevision revision, (List<StpRevisionSection> Sections, List<StpRevisionTest> Tests)? content = null)
    {
        var sections = content?.Sections ?? revision.Sections;
        var tests = content?.Tests ?? revision.Tests;
        if (string.IsNullOrWhiteSpace(revision.Title)) return Error.Validation("StpRevision.Title", "Enter the STP title before submitting.");
        foreach (var kind in new[] { StpSectionKind.Purpose, StpSectionKind.Scope, StpSectionKind.Responsibility, StpSectionKind.Accountability })
            if (!sections.Any(item => item.Kind == kind && !string.IsNullOrWhiteSpace(item.Text)))
                return Error.Validation("StpRevision.Sections", $"Enter the {kind} section before submitting.");
        if (tests.Count == 0) return Error.Validation("StpRevision.Tests", "Add at least one test before submitting.");
        var empty = tests.FirstOrDefault(test => test.Steps.Count == 0);
        if (empty is not null) return Error.Validation("StpRevision.Steps", $"Test {empty.Number} {empty.Title} has no steps.".Trim());
        return null;
    }

    /// <summary>A revision's number stays above the revision it revises; a first revision's above what it supersedes.</summary>
    private async Task<Error?> CheckHeader(StpRevision revision, int revisionNumber, int? supersedes)
    {
        if (revision.RevisedFromId.HasValue)
        {
            var previous = await context.StpRevisions.IgnoreQueryFilters().AsNoTracking().Where(item => item.Id == revision.RevisedFromId)
                .Select(item => new { item.RevisionNumber, item.Status, item.DeletedAt }).FirstOrDefaultAsync();
            if (previous is null || previous.Status != StpRevisionStatus.Approved
                || (previous.DeletedAt.HasValue && await Current(revision.Kind, revision.StpNumber) is not null))
                return Error.Conflict("StpRevision.Current", "The revision this draft revises is no longer the approved one. Delete it and revise the approved revision.");
            if (revisionNumber <= previous.RevisionNumber)
                return Error.Validation("StpRevision.Revision", $"The revision number must be higher than {previous.RevisionNumber}.");
        }
        else if (supersedes is { } before && before >= revisionNumber)
            return Error.Validation("StpRevision.Revision", "The revision number must be higher than the revision it supersedes.");
        return null;
    }

    private static void ApplyHeader(StpRevision revision, StpRevisionContent request, bool firstRevision)
    {
        revision.RevisionNumber = request.RevisionNumber;
        if (firstRevision) revision.SupersedesRevision = request.SupersedesRevision;
        revision.ReasonForChange = Trim(request.ReasonForChange);
        revision.Title = Trim(request.Title);
        revision.Area = Trim(request.Area);
        revision.Reference = Trim(request.Reference);
        revision.EffectiveDate = Utc(request.EffectiveDate);
        revision.ReviewDate = Utc(request.ReviewDate);
        revision.IssueDate = Utc(request.IssueDate);
        if (!string.IsNullOrWhiteSpace(request.Source)) revision.Source = request.Source.Trim();
    }

    /// <summary>
    /// Header dates are calendar dates. A value without a time zone (a plain "2026-11-27", or a date read from a Word
    /// file) is that day in UTC; PostgreSQL's timestamptz refuses an unspecified kind.
    /// </summary>
    internal static DateTime? Utc(DateTime? value) => value switch
    {
        null => null,
        { Kind: DateTimeKind.Utc } date => date,
        { Kind: DateTimeKind.Local } date => date.ToUniversalTime(),
        { } date => DateTime.SpecifyKind(date, DateTimeKind.Utc),
    };

    private static string Trim(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>Test keys this revision and its predecessors have used, so a key cannot be reused for another test.</summary>
    private async Task<HashSet<string>> KnownKeys(StpRevision revision) =>
        (await context.StpRevisions.AsNoTracking()
            .Where(item => item.Kind == revision.Kind && item.StpNumber == revision.StpNumber)
            .SelectMany(item => item.Tests.Select(test => test.TestKey)).ToListAsync())
        .ToHashSet(StringComparer.Ordinal);

    /// <summary>
    /// Checks and builds the content. A test keeps the key it was sent with; a new test gets one from its title. Keys are
    /// lower-case words joined by dashes and unique within the revision.
    /// </summary>
    private static Result<(List<StpRevisionSection> Sections, List<StpRevisionTest> Tests)> BuildContent(
        StpRevisionContent request, HashSet<string> knownKeys, Guid actorId)
    {
        var now = DateTime.UtcNow;
        var sections = new List<StpRevisionSection>();
        foreach (var section in request.Sections ?? [])
        {
            if (!Enum.IsDefined(section.Kind)) return Error.Validation("StpRevision.Sections", "A section has an unknown kind.");
            if (sections.Any(item => item.Kind == section.Kind))
                return Error.Validation("StpRevision.Sections", $"The {section.Kind} section is listed twice.");
            if (section.Text?.Length > MaxText) return Error.Validation("StpRevision.Sections", $"The {section.Kind} section is too long.");
            if (string.IsNullOrWhiteSpace(section.Text)) continue;
            sections.Add(new StpRevisionSection
            {
                Id = Guid.NewGuid(), Kind = section.Kind, Order = (int)section.Kind, Text = section.Text.Trim(),
                CreatedById = actorId, CreatedAt = now,
            });
        }
        var requested = request.Tests ?? [];
        if (requested.Count > MaxTests) return Error.Validation("StpRevision.Tests", $"An STP can hold at most {MaxTests} tests.");
        var used = new HashSet<string>(StringComparer.Ordinal);
        var tests = new List<StpRevisionTest>();
        for (var index = 0; index < requested.Count; index++)
        {
            var row = requested[index];
            var at = $"Test {index + 1}";
            var title = Trim(row.Title);
            if (title is null) return Error.Validation("StpRevision.Tests", $"{at}: enter the test's title.");
            var key = Trim(row.TestKey);
            if (key is not null && !Regex.IsMatch(key, "^[a-z0-9]+(?:-[a-z0-9]+)*$"))
                return Error.Validation("StpRevision.TestKey", $"{at}: the test key may only hold lower-case letters, digits and dashes.");
            if (key is not null && !used.Add(key))
                return Error.Validation("StpRevision.TestKey", $"{at}: the test key {key} is used by another test.");
            if (key is null)
            {
                var stem = StpDocxContentReader.Slug(title) is { Length: > 0 } slug ? slug : $"test-{index + 1}";
                key = stem;
                // A new test never takes a key an earlier revision gave to another test.
                for (var suffix = 2; used.Contains(key) || knownKeys.Contains(key); suffix++) key = $"{stem}-{suffix}";
                used.Add(key);
            }
            var steps = row.Steps ?? [];
            if (steps.Count > MaxSteps) return Error.Validation("StpRevision.Steps", $"{at}: a test can hold at most {MaxSteps} steps.");
            var built = new List<StpRevisionTestStep>();
            for (var position = 0; position < steps.Count; position++)
            {
                var step = steps[position];
                if (!Enum.IsDefined(step.Kind)) return Error.Validation("StpRevision.Steps", $"{at}, step {position + 1}: unknown kind.");
                var text = step.Text?.Trim();
                var heading = Trim(step.Heading);
                if (string.IsNullOrEmpty(text) && heading is null)
                    return Error.Validation("StpRevision.Steps", $"{at}, step {position + 1}: enter the step's text.");
                if (text?.Length > MaxText) return Error.Validation("StpRevision.Steps", $"{at}, step {position + 1}: the text is too long.");
                built.Add(new StpRevisionTestStep
                {
                    Id = Guid.NewGuid(), Order = position + 1, Kind = step.Kind, Number = Trim(step.Number), Heading = heading,
                    Text = string.IsNullOrEmpty(text) ? null : text, ReferenceCode = Trim(step.ReferenceCode),
                    CreatedById = actorId, CreatedAt = now,
                });
            }
            tests.Add(new StpRevisionTest
            {
                Id = Guid.NewGuid(), TestKey = key, Number = Trim(row.Number), Title = title, Order = index + 1, Steps = built,
                CreatedById = actorId, CreatedAt = now,
            });
        }
        return (sections, tests);
    }

    private async Task<Result> Replace(StpRevision revision, (List<StpRevisionSection> Sections, List<StpRevisionTest> Tests) content,
        Guid actorId, StpRevisionTask task)
    {
        var refused = await Refuse(task, revision.Kind, actorId);
        if (refused is not null) return refused;
        revision.UpdatedAt = DateTime.UtcNow;
        revision.LastUpdatedById = actorId;
        revision.Version++;
        context.StpRevisionTestSteps.RemoveRange(revision.Tests.SelectMany(test => test.Steps));
        context.StpRevisionTests.RemoveRange(revision.Tests);
        context.StpRevisionSections.RemoveRange(revision.Sections);
        await context.SaveChangesAsync();
        refused = await Refuse(task, revision.Kind, actorId);
        if (refused is not null) return refused;
        foreach (var section in content.Sections) section.StpRevisionId = revision.Id;
        foreach (var test in content.Tests) test.StpRevisionId = revision.Id;
        context.StpRevisionSections.AddRange(content.Sections);
        context.StpRevisionTests.AddRange(content.Tests);
        await context.SaveChangesAsync();
        return Result.Success();
    }
}
