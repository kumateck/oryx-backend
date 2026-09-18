using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.QcWorksheets;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository.QcWorksheets;

/// <summary>
/// The Analytical Request Document — a testing round and the worksheets it implies.
/// <para>
/// The one rule that shapes everything here is <b>hard version pinning</b>
/// (lifecycle-and-governance.md): a round is pinned to the Specification version in force when
/// it was created, and each of its worksheets to the template version that Specification
/// version linked. Neither is ever resolved forward. Every read in this repository loads the
/// pinned rows by id and nothing in it walks <c>SupersedesId</c>.
/// </para>
/// <para>
/// Additive: nothing here reads or writes the live <c>AnalyticalTestRequest</c> path. Two live
/// tables are read for validation only — <c>MaterialBatches</c> and
/// <c>BatchManufacturingRecords</c> — and neither is modified.
/// </para>
/// </summary>
public class TestRequestRepository(ApplicationDbContext context, IMapper mapper) : ITestRequestRepository
{
    /// <summary>Carries the two counts a round's list row shows without loading its subjects.</summary>
    private sealed class TestRequestListRow
    {
        public TestRequest Request { get; init; }
        public string SpecificationCode { get; init; }
        public string SpecificationName { get; init; }
        public User CreatedBy { get; init; }
        public User IssuedBy { get; init; }
        public int SubjectCount { get; init; }
        public int WorksheetInstanceCount { get; init; }
    }

    // -----------------------------------------------------------------------
    // Reads
    // -----------------------------------------------------------------------

    public async Task<Result<Paginateable<IEnumerable<TestRequestSummaryDto>>>> GetTestRequests(
        int page, int pageSize, string searchQuery, TestRequestStatus? status, TestRequestType? type,
        DateTime? from, DateTime? to)
    {
        var query = context.QcTestRequests.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            var term = searchQuery.Trim().ToLower();
            query = query.Where(item =>
                item.ArNumber.ToLower().Contains(term)
                || item.Subjects.Any(subject => subject.SubjectRef.ToLower().Contains(term)));
        }

        if (status.HasValue) query = query.Where(item => item.Status == status.Value);
        if (type.HasValue) query = query.Where(item => item.Type == type.Value);
        if (from.HasValue) query = query.Where(item => item.CreatedAt >= from.Value);
        if (to.HasValue) query = query.Where(item => item.CreatedAt <= to.Value);

        // A round can carry 90 subjects, each with its own worksheets, so the list projects the
        // two counts rather than loading the graph to count it in memory.
        var projected = query
            .Select(item => new TestRequestListRow
            {
                Request = item,
                SpecificationCode = item.Specification.Code,
                SpecificationName = item.Specification.Name,
                CreatedBy = item.CreatedBy,
                IssuedBy = item.IssuedBy,
                SubjectCount = item.Subjects.Count,
                WorksheetInstanceCount = item.Subjects.Sum(subject => subject.WorksheetInstances.Count)
            })
            .OrderByDescending(item => item.Request.CreatedAt);

        return await PaginationHelper.GetPaginatedResultAsync(projected, page, pageSize, ToSummaryDto);
    }

    public async Task<Result<TestRequestDetailDto>> GetTestRequest(Guid id)
    {
        var request = await LoadDetail(id);
        return request is null
            ? Result.Failure<TestRequestDetailDto>(QcWorksheetErrors.TestRequestNotFound(id))
            : Result.Success(ToDetailDto(request));
    }

    // -----------------------------------------------------------------------
    // Writes
    // -----------------------------------------------------------------------

    public async Task<Result<TestRequestDetailDto>> CreateTestRequest(
        CreateTestRequestRequest request, Guid userId)
    {
        if (!request.Type.HasValue || !Enum.IsDefined(request.Type.Value))
            return Result.Failure<TestRequestDetailDto>(
                Error.Validation("QcTestRequest.TypeRequired", "A test request must declare its type."));

        if (!request.ScheduleOrigin.HasValue || !Enum.IsDefined(request.ScheduleOrigin.Value))
            return Result.Failure<TestRequestDetailDto>(
                Error.Validation(
                    "QcTestRequest.ScheduleOriginRequired",
                    "A test request must declare whether it is scheduled or unscheduled."));

        var type = request.Type.Value;
        var origin = request.ScheduleOrigin.Value;

        // Mandatory reason on an unscheduled round, and no stray reason on a scheduled one.
        if (origin == TestRequestScheduleOrigin.Unscheduled
            && string.IsNullOrWhiteSpace(request.UnscheduledReason))
            return Result.Failure<TestRequestDetailDto>(QcWorksheetErrors.UnscheduledReasonRequired);

        if (origin == TestRequestScheduleOrigin.Scheduled
            && !string.IsNullOrWhiteSpace(request.UnscheduledReason))
            return Result.Failure<TestRequestDetailDto>(QcWorksheetErrors.UnscheduledReasonNotApplicable);

        var subjects = request.Subjects ?? [];
        if (subjects.Count == 0)
            return Result.Failure<TestRequestDetailDto>(QcWorksheetErrors.TestRequestHasNoSubjects);

        var specificationResult = await LoadPinnedSpecification(request.SpecificationId, type);
        if (!specificationResult.IsSuccess)
            return Result.Failure<TestRequestDetailDto>(specificationResult.Error);

        var specification = specificationResult.Value;

        foreach (var subject in subjects)
        {
            var validation = await ValidateSubject(subject, type);
            if (!validation.IsSuccess)
                return Result.Failure<TestRequestDetailDto>(validation.Error);
        }

        var testRequest = new TestRequest
        {
            Id = Guid.NewGuid(),
            Type = type,
            SpecificationId = specification.Id,

            // Pinned here, once, from the specification row itself. Never client-supplied and
            // never recomputed later.
            SpecificationVersion = specification.Version,
            ScheduleOrigin = origin,
            UnscheduledReason = request.UnscheduledReason?.Trim(),
            ArNumber = request.ArNumber?.Trim(),
            IssueNumber = request.IssueNumber?.Trim(),
            IssuedAt = request.IssuedAt,
            IssuedById = userId,
            Status = TestRequestStatus.Draft,
            CreatedAt = DateTime.UtcNow,
            CreatedById = userId
        };

        foreach (var subjectRequest in subjects)
            testRequest.Subjects.Add(BuildSubject(subjectRequest, specification, userId));

        context.QcTestRequests.Add(testRequest);

        // One transaction: a round never exists without the worksheets it implies.
        await context.SaveChangesAsync();

        return await GetTestRequest(testRequest.Id);
    }

    public async Task<Result<TestRequestDetailDto>> AddSubjects(
        Guid id, AddTestRequestSubjectsRequest request, Guid userId)
    {
        var testRequest = await context.QcTestRequests.SingleOrDefaultAsync(item => item.Id == id);
        if (testRequest is null)
            return Result.Failure<TestRequestDetailDto>(QcWorksheetErrors.TestRequestNotFound(id));

        // Points get added incrementally on an EM/Water round, but only until testing starts.
        if (testRequest.Status is not (TestRequestStatus.Draft or TestRequestStatus.Sampled))
            return Result.Failure<TestRequestDetailDto>(
                QcWorksheetErrors.SubjectsLockedAfterSampling(testRequest.Status));

        var subjects = request.Subjects ?? [];
        if (subjects.Count == 0)
            return Result.Failure<TestRequestDetailDto>(QcWorksheetErrors.TestRequestHasNoSubjects);

        // The round's own pinned specification row, by id — not whichever version is Effective
        // now. A subject added late runs the same worksheets as the ones added at creation.
        var specification = await LoadSpecificationGraph(testRequest.SpecificationId);
        if (specification is null)
            return Result.Failure<TestRequestDetailDto>(
                QcWorksheetErrors.TestRequestSpecificationNotFound(testRequest.SpecificationId));

        foreach (var subject in subjects)
        {
            var validation = await ValidateSubject(subject, testRequest.Type);
            if (!validation.IsSuccess)
                return Result.Failure<TestRequestDetailDto>(validation.Error);
        }

        foreach (var subjectRequest in subjects)
        {
            var subject = BuildSubject(subjectRequest, specification, userId);
            subject.TestRequestId = testRequest.Id;
            context.QcTestRequestSubjects.Add(subject);
        }

        testRequest.UpdatedAt = DateTime.UtcNow;
        testRequest.LastUpdatedById = userId;

        await context.SaveChangesAsync();

        return await GetTestRequest(id);
    }

    public async Task<Result<TestRequestDetailDto>> RecordSample(
        Guid id, RecordTestRequestSampleRequest request, Guid userId)
    {
        var testRequest = await context.QcTestRequests
            .Include(item => item.Subjects)
            .SingleOrDefaultAsync(item => item.Id == id);

        if (testRequest is null)
            return Result.Failure<TestRequestDetailDto>(QcWorksheetErrors.TestRequestNotFound(id));

        if (testRequest.Status != TestRequestStatus.Draft)
            return Result.Failure<TestRequestDetailDto>(
                QcWorksheetErrors.RecordSampleRequiresDraft(testRequest.Status));

        var requested = request.SubjectIds ?? [];
        var targets = requested.Count == 0
            ? testRequest.Subjects.ToList()
            : testRequest.Subjects.Where(subject => requested.Contains(subject.Id)).ToList();

        if (requested.Count > 0)
        {
            var missing = requested.Except(targets.Select(subject => subject.Id)).ToList();
            if (missing.Count > 0)
                return Result.Failure<TestRequestDetailDto>(
                    QcWorksheetErrors.TestRequestSubjectNotFound(missing[0]));
        }

        if (targets.Count == 0)
            return Result.Failure<TestRequestDetailDto>(QcWorksheetErrors.TestRequestHasNoSubjects);

        var collectedAt = request.CollectedAt ?? DateTime.UtcNow;

        foreach (var subject in targets)
        {
            subject.CollectedAt = collectedAt;
            subject.UpdatedAt = DateTime.UtcNow;
            subject.LastUpdatedById = userId;
        }

        testRequest.Status = TestRequestStatus.Sampled;
        testRequest.UpdatedAt = DateTime.UtcNow;
        testRequest.LastUpdatedById = userId;

        await context.SaveChangesAsync();

        return await GetTestRequest(id);
    }

    // -----------------------------------------------------------------------
    // Validation
    // -----------------------------------------------------------------------

    /// <summary>
    /// Loads the exact Specification version row the round will pin to — the row the given id
    /// names, with no walk forward through <c>SupersedesId</c> — and checks it can actually
    /// govern this round.
    /// </summary>
    private async Task<Result<Specification>> LoadPinnedSpecification(Guid specificationId, TestRequestType type)
    {
        var specification = await LoadSpecificationGraph(specificationId);

        if (specification is null)
            return Result.Failure<Specification>(
                QcWorksheetErrors.TestRequestSpecificationNotFound(specificationId));

        // Real testing runs against a controlled document that somebody signed for. A Draft or
        // UnderReview specification has no approved acceptance criteria behind it.
        if (specification.Status != QcDocumentStatus.Effective)
            return Result.Failure<Specification>(
                QcWorksheetErrors.SpecificationNotEffective(specification.Code, specification.Status));

        var expected = QcTestRequestTypes.ToAppliesTo(type);
        if (specification.AppliesTo != expected)
            return Result.Failure<Specification>(
                QcWorksheetErrors.SpecificationTypeMismatch(type, specification.AppliesTo));

        if (specification.WorksheetLinks.Count == 0)
            return Result.Failure<Specification>(
                QcWorksheetErrors.SpecificationHasNoWorksheetLinks(specification.Code));

        return Result.Success(specification);
    }

    private async Task<Result> ValidateSubject(CreateTestRequestSubjectRequest subject, TestRequestType type)
    {
        if (string.IsNullOrWhiteSpace(subject.SubjectRef))
            return Error.Validation(
                "QcTestRequest.SubjectRefRequired",
                "Every subject must carry a batch number or sampling point code.");

        // Each optional link belongs to exactly one category of round. Setting one on the wrong
        // category would be meaningless data that a later COA or OOS case could read.
        if (subject.SamplingPointGroupId.HasValue)
        {
            if (!QcTestRequestTypes.IsRoutine(type))
                return QcWorksheetErrors.SamplingPointGroupIsRoutineOnly(type);

            if (!await context.QcSamplingPointGroups.AnyAsync(
                    item => item.Id == subject.SamplingPointGroupId.Value))
                return QcWorksheetErrors.SamplingPointGroupNotFoundOnSubject(subject.SamplingPointGroupId.Value);
        }

        if (subject.MaterialBatchId.HasValue)
        {
            if (!QcTestRequestTypes.IsMaterial(type))
                return QcWorksheetErrors.MaterialBatchIsMaterialOnly(type);

            if (!await context.MaterialBatches.AnyAsync(item => item.Id == subject.MaterialBatchId.Value))
                return QcWorksheetErrors.MaterialBatchNotFound(subject.MaterialBatchId.Value);
        }

        if (subject.BatchManufacturingRecordId.HasValue)
        {
            if (type != TestRequestType.Product)
                return QcWorksheetErrors.BatchManufacturingRecordIsProductOnly(type);

            if (!await context.BatchManufacturingRecords.AnyAsync(
                    item => item.Id == subject.BatchManufacturingRecordId.Value))
                return QcWorksheetErrors.BatchManufacturingRecordNotFound(
                    subject.BatchManufacturingRecordId.Value);
        }

        return Result.Success();
    }

    // -----------------------------------------------------------------------
    // Materialization
    // -----------------------------------------------------------------------

    /// <summary>
    /// Builds a Subject together with its worksheets: one <see cref="WorksheetInstance"/> per
    /// WorksheetLink on the pinned Specification version. A specification linking both a
    /// Chemical and a Microbial template therefore gives every subject two worksheets, which
    /// is what "RequiredWorksheets" means — resolved once, here, and never stored as a list.
    /// </summary>
    private static TestRequestSubject BuildSubject(
        CreateTestRequestSubjectRequest request, Specification specification, Guid userId)
    {
        var subjectId = Guid.NewGuid();

        return new TestRequestSubject
        {
            Id = subjectId,
            SubjectRef = request.SubjectRef?.Trim(),
            SubjectLabel = request.SubjectLabel?.Trim(),
            ArNumber = request.ArNumber?.Trim(),
            SamplingPointGroupId = request.SamplingPointGroupId,
            MaterialBatchId = request.MaterialBatchId,
            BatchManufacturingRecordId = request.BatchManufacturingRecordId,
            CollectedAt = request.CollectedAt,
            CreatedAt = DateTime.UtcNow,
            CreatedById = userId,
            WorksheetInstances = specification.WorksheetLinks
                .OrderBy(link => link.AnalysisType)
                .Select(link => new WorksheetInstance
                {
                    Id = Guid.NewGuid(),
                    TestRequestSubjectId = subjectId,
                    WorksheetTemplateId = link.WorksheetTemplateId,

                    // The link's own pin is copied straight across — the template version the
                    // Specification was approved against, not whichever version is Effective
                    // now. The fallback reads the version off the row the link already points
                    // at (the same immutable row), which keeps this total for links written
                    // before the pin column existed; it is not a forward resolution.
                    WorksheetTemplateVersion = link.WorksheetTemplateVersion > 0
                        ? link.WorksheetTemplateVersion
                        : link.WorksheetTemplate?.Version ?? 0,
                    AnalysisType = link.AnalysisType,
                    Status = WorksheetInstanceStatus.NotStarted,
                    Approved = false,
                    CreatedAt = DateTime.UtcNow,
                    CreatedById = userId
                })
                .ToList()
        };
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private async Task<Specification> LoadSpecificationGraph(Guid specificationId) =>
        await context.QcSpecifications
            .AsNoTracking()
            .Include(item => item.WorksheetLinks)
                .ThenInclude(link => link.WorksheetTemplate)
            .AsSplitQuery()
            .SingleOrDefaultAsync(item => item.Id == specificationId);

    /// <summary>
    /// Read path, no-tracking for the same reason Milestone 2's is: the context soft-deletes by
    /// turning a Deleted entry into a Modified one, so removed children would otherwise be
    /// fixed back into the graph on a read-after-write.
    /// </summary>
    private async Task<TestRequest> LoadDetail(Guid id) =>
        await context.QcTestRequests
            .AsNoTracking()
            .Include(item => item.CreatedBy)
            .Include(item => item.IssuedBy)
            .Include(item => item.Specification)
            .Include(item => item.Subjects)
                .ThenInclude(subject => subject.SamplingPointGroup)
            .Include(item => item.Subjects)
                .ThenInclude(subject => subject.WorksheetInstances)
                    .ThenInclude(instance => instance.WorksheetTemplate)
            .Include(item => item.Subjects)
                .ThenInclude(subject => subject.WorksheetInstances)
                    .ThenInclude(instance => instance.AssignedTo)
            .AsSplitQuery()
            .SingleOrDefaultAsync(item => item.Id == id);

    private TestRequestSummaryDto ToSummaryDto(TestRequestListRow row) => new()
    {
        Id = row.Request.Id,
        Type = row.Request.Type,
        SpecificationId = row.Request.SpecificationId,
        SpecificationVersion = row.Request.SpecificationVersion,
        SpecificationCode = row.SpecificationCode,
        SpecificationName = row.SpecificationName,
        ScheduleOrigin = row.Request.ScheduleOrigin,
        UnscheduledReason = row.Request.UnscheduledReason,
        ArNumber = row.Request.ArNumber,
        IssueNumber = row.Request.IssueNumber,
        IssuedAt = row.Request.IssuedAt,
        IssuedBy = mapper.Map<UserDto>(row.IssuedBy),
        Status = row.Request.Status,
        SubjectCount = row.SubjectCount,
        WorksheetInstanceCount = row.WorksheetInstanceCount,
        CreatedAt = row.Request.CreatedAt,
        CreatedBy = mapper.Map<UserDto>(row.CreatedBy)
    };

    private TestRequestDetailDto ToDetailDto(TestRequest request) => new()
    {
        Id = request.Id,
        Type = request.Type,
        SpecificationId = request.SpecificationId,

        // The pinned version, reported as stored — deliberately not re-read from the
        // specification row, so a drifted pin would be visible rather than papered over.
        SpecificationVersion = request.SpecificationVersion,
        SpecificationCode = request.Specification?.Code,
        SpecificationName = request.Specification?.Name,
        ScheduleOrigin = request.ScheduleOrigin,
        UnscheduledReason = request.UnscheduledReason,
        ArNumber = request.ArNumber,
        IssueNumber = request.IssueNumber,
        IssuedAt = request.IssuedAt,
        IssuedBy = mapper.Map<UserDto>(request.IssuedBy),
        Status = request.Status,
        SubjectCount = request.Subjects.Count,
        WorksheetInstanceCount = request.Subjects.Sum(subject => subject.WorksheetInstances.Count),
        CreatedAt = request.CreatedAt,
        CreatedBy = mapper.Map<UserDto>(request.CreatedBy),
        Subjects = request.Subjects
            .OrderBy(subject => subject.CreatedAt)
            .ThenBy(subject => subject.SubjectRef)
            .Select(subject => new TestRequestSubjectDto
            {
                Id = subject.Id,
                TestRequestId = subject.TestRequestId,
                SubjectRef = subject.SubjectRef,
                SubjectLabel = subject.SubjectLabel,
                ArNumber = subject.ArNumber,
                SamplingPointGroupId = subject.SamplingPointGroupId,
                SamplingPointGroup = subject.SamplingPointGroup is null
                    ? null
                    : new SamplingPointGroupDto
                    {
                        Id = subject.SamplingPointGroup.Id,
                        Name = subject.SamplingPointGroup.Name,
                        Description = subject.SamplingPointGroup.Description,
                        CreatedAt = subject.SamplingPointGroup.CreatedAt
                    },
                SamplingPointId = subject.SamplingPointId,
                MaterialBatchId = subject.MaterialBatchId,
                BatchManufacturingRecordId = subject.BatchManufacturingRecordId,
                CollectedAt = subject.CollectedAt,
                CreatedAt = subject.CreatedAt,
                WorksheetInstances = subject.WorksheetInstances
                    .OrderBy(instance => instance.AnalysisType)
                    .Select(instance => QcWorksheetInstanceMapper.ToSummaryDto(instance, mapper))
                    .ToList()
            })
            .ToList()
    };
}
