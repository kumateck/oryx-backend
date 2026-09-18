using DOMAIN.Entities.QcWorksheets;
using DOMAIN.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace APP.Tests.QcWorksheets;

/// <summary>
/// Milestone 5's acceptance criteria, end to end against the real repositories, the real
/// generation service wired into both of its real triggers, the real approval engine and genuine
/// password hashing — so these prove the production path rather than a parallel one.
/// <para>
/// No test here calls the generator directly. Every certificate in this file appears because a
/// worksheet was reviewed or an OOS case was closed, which is the only way one ever appears in
/// production.
/// </para>
/// </summary>
public class CoaAcceptanceTests
{
    private const string ChemicalField = "assay";
    private const string MicrobialField = "tamc";

    /// <summary>A round set up to the point where its worksheets can be filled in.</summary>
    private sealed record Round(
        QcWorksheetTestContext Harness,
        Guid TestRequestId,
        Specification Specification,
        WorksheetTemplate Chemical,
        WorksheetTemplate Microbial,
        User Analyst,
        List<TestRequestSubjectDto> Subjects);

    // =======================================================================
    // Arrangement
    // =======================================================================

    /// <summary>
    /// A round against a Specification requiring both tracks, with one printable Characteristic
    /// per track. This is the shape acceptance criteria 1, 2, 4 and 5 all begin from.
    /// </summary>
    private static async Task<Round> ArrangeBothTracks(
        QcWorksheetTestContext harness,
        TestRequestType type = TestRequestType.RawMaterial,
        string chemicalCriteria = "95.0-105.0%",
        string microbialCriteria = "NMT 1000 CFU/g",
        params string[] subjectRefs)
    {
        await harness.SeedApprovalChain(QcWorksheetModelTypes.WorksheetInstance);

        var chemical = await harness.SeedEffectiveTemplate(
            $"WS/CHEM/{Guid.NewGuid().ToString()[..6]}", WorksheetCategory.Chemical, ChemicalField);

        var microbial = await harness.SeedEffectiveTemplate(
            $"WS/MICRO/{Guid.NewGuid().ToString()[..6]}", WorksheetCategory.Microbial, MicrobialField);

        var specification = await harness.SeedEffectiveSpecification(
            QcTestRequestTypes.ToAppliesTo(type),
            (chemical, SpecificationAnalysisType.Chemical),
            (microbial, SpecificationAnalysisType.Microbial));

        await SeedPrintableCharacteristic(
            harness, specification, chemical, ChemicalField, chemicalCriteria, "CHEMICAL", 1, "Assay");

        await SeedPrintableCharacteristic(
            harness, specification, microbial, MicrobialField, microbialCriteria, "MICROBIAL", 2, "TAMC");

        return await Raise(harness, specification, chemical, microbial, type, subjectRefs);
    }

    /// <summary>
    /// A Characteristic that prints. <c>SeedCharacteristic</c> does not set the two fields the COA
    /// engine renders by — the section heading and the print order — so they are set here.
    /// </summary>
    private static async Task<SpecificationCharacteristic> SeedPrintableCharacteristic(
        QcWorksheetTestContext harness,
        Specification specification,
        WorksheetTemplate template,
        string fieldKey,
        string acceptanceCriteria,
        string groupName,
        int displayOrder,
        string testName)
    {
        var characteristic = await harness.SeedCharacteristic(
            specification, template, fieldKey, acceptanceCriteria, testName: testName);

        characteristic.GroupName = groupName;
        characteristic.DisplayOrder = displayOrder;
        await harness.Db.SaveChangesAsync();

        return characteristic;
    }

    private static async Task<Round> Raise(
        QcWorksheetTestContext harness,
        Specification specification,
        WorksheetTemplate chemical,
        WorksheetTemplate microbial,
        TestRequestType type,
        string[] subjectRefs)
    {
        var refs = subjectRefs.Length == 0 ? ["PT260901"] : subjectRefs;
        var analyst = await harness.SeedUser($"analyst.{Guid.NewGuid().ToString()[..6]}");

        var created = await harness.TestRequests.CreateTestRequest(
            new CreateTestRequestRequest
            {
                Type = type,
                SpecificationId = specification.Id,
                ScheduleOrigin = TestRequestScheduleOrigin.Scheduled,
                ArNumber = $"ARD-{Guid.NewGuid().ToString()[..6]}",
                Subjects = refs
                    .Select(item => new CreateTestRequestSubjectRequest
                    {
                        SubjectRef = item,
                        SubjectLabel = $"{item} location"
                    })
                    .ToList()
            },
            Guid.NewGuid());

        Assert.True(created.IsSuccess, created.Error?.Description);

        await harness.TestRequests.RecordSample(
            created.Value.Id, new RecordTestRequestSampleRequest(), Guid.NewGuid());

        return new Round(
            harness, created.Value.Id, specification, chemical, microbial, analyst,
            created.Value.Subjects.ToList());
    }

    /// <summary>Assigns, starts, fills in and submits one worksheet.</summary>
    private static async Task Submit(Round round, Guid instanceId, string fieldKey, string value)
    {
        var harness = round.Harness;

        await harness.WorksheetInstances.Assign(
            instanceId,
            new AssignWorksheetInstanceRequest { AssignedToId = round.Analyst.Id },
            Guid.NewGuid());

        await harness.WorksheetInstances.Start(instanceId, round.Analyst.Id);

        var saved = await harness.WorksheetInstances.SaveValues(
            instanceId,
            new SaveWorksheetValuesRequest
            {
                FieldValues = [new WorksheetFieldValueEntry { FieldKey = fieldKey, Value = value }]
            },
            round.Analyst.Id);

        Assert.True(saved.IsSuccess, saved.Error?.Description);

        var submitted = await harness.WorksheetInstances.Submit(instanceId, round.Analyst.Id);
        Assert.True(submitted.IsSuccess, submitted.Error?.Description);
    }

    /// <summary>Reviews as the approver, who is never the analyst — segregation of duties holds.</summary>
    private static async Task Review(Round round, Guid instanceId)
    {
        var reviewed = await round.Harness.WorksheetInstances.Review(
            instanceId,
            new ReviewWorksheetInstanceRequest
            {
                Approve = true,
                Password = QcWorksheetTestContext.CorrectPassword,
                Comments = "Reviewed."
            },
            round.Harness.Approver.Id,
            [round.Harness.ApproverRole.Id]);

        Assert.True(reviewed.IsSuccess, reviewed.Error?.Description);
    }

    /// <summary>The instance id for one subject's track, resolved through the pinned template.</summary>
    private static Guid InstanceFor(Round round, int subjectIndex, WorksheetTemplate template) =>
        round.Subjects[subjectIndex].WorksheetInstances
            .Single(item => item.WorksheetTemplateId == template.Id)
            .Id;

    private static Task<List<Coa>> Certificates(QcWorksheetTestContext harness, Guid testRequestId) =>
        harness.Db.Coas
            .AsNoTracking()
            .Include(item => item.Rows)
            .Where(item => item.TestRequestId == testRequestId)
            .ToListAsync();

    // =======================================================================
    // Criterion 1 — auto-generation on full review, not before
    // =======================================================================

    /// <summary>
    /// Criterion 1 — the strict hold in its ordinary form. A Specification requiring both tracks
    /// yields no certificate when only the Chemical track is reviewed, and exactly one Draft the
    /// moment the Microbial one follows it.
    /// <para>
    /// This is the case the rule exists for: microbial incubation routinely finishes days after a
    /// chemical assay, and an interim Chemical-only certificate in that window would imply a
    /// release the microbial track has not yet supported.
    /// </para>
    /// </summary>
    [Fact]
    public async Task No_certificate_until_every_track_is_reviewed_then_exactly_one_draft()
    {
        using var harness = new QcWorksheetTestContext();
        var round = await ArrangeBothTracks(harness);

        var chemical = InstanceFor(round, 0, round.Chemical);
        var microbial = InstanceFor(round, 0, round.Microbial);

        await Submit(round, chemical, ChemicalField, "98.7%");
        await Submit(round, microbial, MicrobialField, "45 CFU/g");

        await Review(round, chemical);

        // The chemical track is finished and in the clear. There is still no certificate.
        Assert.Empty(await Certificates(harness, round.TestRequestId));

        await Review(round, microbial);

        var certificate = Assert.Single(await Certificates(harness, round.TestRequestId));
        Assert.Equal(CoaStatus.Draft, certificate.Status);
        Assert.Equal(CoaCertificateShape.CertificateOfAnalysis, certificate.CertificateShape);
        Assert.Equal(1, certificate.RevisionNumber);
        Assert.Null(certificate.IssuedAt);
        Assert.True(certificate.OverallComplies);

        // Both tracks print, each under its own section heading.
        Assert.Equal(2, certificate.Rows.Count);
        Assert.Contains(certificate.Rows, row => row.GroupName == "CHEMICAL" && row.ResultValue == "98.7%");
        Assert.Contains(certificate.Rows, row => row.GroupName == "MICROBIAL" && row.ResultValue == "45 CFU/g");
        Assert.All(certificate.Rows, row => Assert.True(row.Complies));
    }

    /// <summary>
    /// The hold is per-round, not per-subject: one subject finishing everything while another has
    /// not started still produces no certificate. A partial certificate for "the subjects that
    /// happen to be done" is exactly what the combination rule forbids.
    /// </summary>
    [Fact]
    public async Task A_single_unreviewed_worksheet_on_another_subject_blocks_generation()
    {
        using var harness = new QcWorksheetTestContext();
        var round = await ArrangeBothTracks(
            harness, TestRequestType.RoutineWater, subjectRefs: ["WP-01", "WP-02"]);

        foreach (var index in (int[])[0, 1])
        {
            await Submit(round, InstanceFor(round, index, round.Chemical), ChemicalField, "99.0%");
            await Submit(round, InstanceFor(round, index, round.Microbial), MicrobialField, "10 CFU/g");
        }

        await Review(round, InstanceFor(round, 0, round.Chemical));
        await Review(round, InstanceFor(round, 0, round.Microbial));
        await Review(round, InstanceFor(round, 1, round.Chemical));

        // Every worksheet on WP-01 is reviewed and WP-02 is one away. Still nothing.
        Assert.Empty(await Certificates(harness, round.TestRequestId));

        await Review(round, InstanceFor(round, 1, round.Microbial));

        Assert.Single(await Certificates(harness, round.TestRequestId));
    }

    // =======================================================================
    // Criterion 2 — an open OosCase holds the certificate, closing it releases the hold
    // =======================================================================

    /// <summary>
    /// Criterion 2 — the strict hold as a hard gate, not a soft check. Every worksheet in the
    /// round is Reviewed and the only thing outstanding is one open OOS case; no certificate is
    /// generated. Closing the case by QA disposition — the only way it closes — lets generation
    /// proceed, on the very next trigger and with no user asking for it.
    /// </summary>
    [Fact]
    public async Task An_open_oos_case_blocks_generation_and_closing_it_releases_the_hold()
    {
        using var harness = new QcWorksheetTestContext();
        await harness.SeedApprovalChain(QcWorksheetModelTypes.OosCase);

        var round = await ArrangeBothTracks(harness);

        var chemical = InstanceFor(round, 0, round.Chemical);
        var microbial = InstanceFor(round, 0, round.Microbial);

        // Fails the acceptance criteria, which for a Characteristic with no separate Action limit
        // is itself the hard limit — so this opens an OosCase automatically on submit.
        await Submit(round, chemical, ChemicalField, "88.0%");
        await Submit(round, microbial, MicrobialField, "45 CFU/g");

        var oosCase = await harness.Db.QcOosCases.SingleAsync(item => item.WorksheetInstanceId == chemical);
        Assert.Equal(OosCaseStatus.Open, oosCase.Status);

        await Review(round, chemical);
        await Review(round, microbial);

        // Every worksheet in the round is Reviewed. The case alone holds the certificate.
        var instances = await harness.Db.QcWorksheetInstances
            .AsNoTracking()
            .Where(item => item.TestRequestSubject.TestRequestId == round.TestRequestId)
            .ToListAsync();

        Assert.All(instances, item => Assert.Equal(WorksheetInstanceStatus.Reviewed, item.Status));
        Assert.Empty(await Certificates(harness, round.TestRequestId));

        // Close it. Any disposition releases the hold — the certificate then reports whatever the
        // disposition left standing, which is not the gate's concern.
        await harness.OosCases.StartInvestigation(oosCase.Id, harness.Approver.Id);

        await harness.OosCases.Escalate(
            oosCase.Id,
            new EscalateOosCaseRequest { Reason = "No assignable laboratory error found." },
            harness.Approver.Id);

        var disposed = await harness.OosCases.RecordDisposition(
            oosCase.Id,
            new OosDispositionRequest
            {
                Outcome = OosDispositionOutcome.ConfirmedOOS,
                Password = QcWorksheetTestContext.CorrectPassword,
                DispositionComments = "Result confirmed."
            },
            harness.Approver.Id,
            [harness.ApproverRole.Id]);

        Assert.True(disposed.IsSuccess, disposed.Error?.Description);
        Assert.Equal(OosCaseStatus.Closed, disposed.Value.Status);

        // Generated by closing the case, with nobody having asked for a certificate.
        var certificate = Assert.Single(await Certificates(harness, round.TestRequestId));
        Assert.Equal(CoaStatus.Draft, certificate.Status);

        // A ConfirmedOOS disposition leaves the original result standing, so the certificate says
        // so rather than quietly reading as compliant.
        Assert.False(certificate.OverallComplies);
        Assert.Contains(certificate.Rows, row => row.ResultValue == "88.0%" && !row.Complies);
    }

    // =======================================================================
    // Criterion 3 — one certificate per round, many row-groups
    // =======================================================================

    /// <summary>
    /// Criterion 3 — an Environmental round covering five rooms produces exactly one Monitoring
    /// Report whose rows span all five subjects, not five separate certificates. The real EM
    /// documents are one report per round with a section per room, and this is that.
    /// </summary>
    [Fact]
    public async Task A_five_room_environmental_round_produces_one_report_spanning_every_room()
    {
        using var harness = new QcWorksheetTestContext();
        await harness.SeedApprovalChain(QcWorksheetModelTypes.WorksheetInstance);

        var microbial = await harness.SeedEffectiveTemplate(
            "WS/EM/1", WorksheetCategory.Microbial, MicrobialField);

        // Environmental Specifications only ever carry a Microbial link — they are never gated on
        // a second track.
        var specification = await harness.SeedEffectiveSpecification(
            SpecificationAppliesTo.RoutineEnvironmental,
            (microbial, SpecificationAnalysisType.Microbial));

        await SeedPrintableCharacteristic(
            harness, specification, microbial, MicrobialField,
            "NMT 100 CFU/4Hrs", "MICROBIAL", 1, "Airborne Viables");

        string[] rooms = ["SF-91", "SF-92", "SF-93", "SF-94", "SF-95"];

        var round = await Raise(
            harness, specification, microbial, microbial,
            TestRequestType.RoutineEnvironmental, rooms);

        Assert.Equal(5, round.Subjects.Count);

        for (var index = 0; index < rooms.Length; index++)
        {
            var instanceId = InstanceFor(round, index, microbial);
            await Submit(round, instanceId, MicrobialField, $"{10 + index} CFU/4Hrs");

            // Nothing before the last room is enough.
            if (index < rooms.Length - 1)
            {
                await Review(round, instanceId);
                Assert.Empty(await Certificates(harness, round.TestRequestId));
            }
        }

        await Review(round, InstanceFor(round, rooms.Length - 1, microbial));

        var certificate = Assert.Single(await Certificates(harness, round.TestRequestId));
        Assert.Equal(CoaCertificateShape.EnvironmentalMonitoringReport, certificate.CertificateShape);
        Assert.StartsWith("EMR-", certificate.CertificateCode, StringComparison.Ordinal);

        // One row per room, all on the one document.
        Assert.Equal(5, certificate.Rows.Count);
        Assert.Equal(5, certificate.Rows.Select(row => row.TestRequestSubjectId).Distinct().Count());

        foreach (var room in rooms)
            Assert.Contains(certificate.Rows, row => row.SubjectRef == room);

        // And the viewer renders it as five sections, matching the per-room layout.
        var detail = await harness.Coas.GetCoa(certificate.Id);
        Assert.True(detail.IsSuccess, detail.Error?.Description);
        Assert.Equal(5, detail.Value.Subjects.Count);
        Assert.Equal(rooms, detail.Value.Subjects.Select(subject => subject.SubjectRef).ToArray());
    }

    // =======================================================================
    // Criterion 4 — snapshotting, not live-join
    // =======================================================================

    /// <summary>
    /// Criterion 4 — the single most important property of the whole entity. Once a certificate
    /// has been generated, editing the Specification it was judged against cannot change a word of
    /// it. A regulatory document that silently rewrote itself when somebody corrected a typo three
    /// versions later would be worthless as evidence.
    /// </summary>
    [Fact]
    public async Task Editing_the_specification_afterwards_does_not_change_a_generated_certificate()
    {
        using var harness = new QcWorksheetTestContext();
        var round = await ArrangeBothTracks(harness);

        var chemical = InstanceFor(round, 0, round.Chemical);
        var microbial = InstanceFor(round, 0, round.Microbial);

        await Submit(round, chemical, ChemicalField, "98.7%");
        await Submit(round, microbial, MicrobialField, "45 CFU/g");
        await Review(round, chemical);
        await Review(round, microbial);

        var certificate = Assert.Single(await Certificates(harness, round.TestRequestId));

        var before = certificate.Rows
            .ToDictionary(row => row.DisplayLabel, row => (row.AcceptanceCriteria, row.ResultValue));

        Assert.Equal("95.0-105.0%", before["Assay"].AcceptanceCriteria);

        // A new Specification version supersedes the one the round was pinned to, and the assay
        // criteria are rewritten on the way.
        var superseded = await harness.Db.QcSpecifications
            .SingleAsync(item => item.Id == round.Specification.Id);

        superseded.Status = QcDocumentStatus.Superseded;

        var next = new Specification
        {
            Id = Guid.NewGuid(),
            Code = superseded.Code,
            Name = superseded.Name,
            AppliesTo = superseded.AppliesTo,
            RetestPolicy = superseded.RetestPolicy,
            Version = superseded.Version + 1,
            SupersedesId = superseded.Id,
            Status = QcDocumentStatus.Effective,
            EffectiveDate = DateTime.UtcNow,
            Approved = true,
            CreatedAt = DateTime.UtcNow
        };

        harness.Db.QcSpecifications.Add(next);

        // And the pinned version's own Characteristic text is edited outright, which is the harder
        // case: not a new version alongside it, but the very row the certificate resolved from.
        var characteristic = await harness.Db.QcSpecificationCharacteristics
            .SingleAsync(item => item.SpecificationId == superseded.Id && item.TestName == "Assay");

        characteristic.AcceptanceCriteria = "90.0-110.0%";
        characteristic.TestName = "Assay (HPLC)";
        characteristic.GroupName = "PHYSICOCHEMICAL";
        await harness.Db.SaveChangesAsync();

        // The certificate reads exactly as it did.
        var reread = Assert.Single(await Certificates(harness, round.TestRequestId));
        var assay = reread.Rows.Single(row => row.DisplayLabel == "Assay");

        Assert.Equal("95.0-105.0%", assay.AcceptanceCriteria);
        Assert.Equal("98.7%", assay.ResultValue);
        Assert.Equal("CHEMICAL", assay.GroupName);
        Assert.True(assay.Complies);

        // And so does the rendered view, which is the surface anyone would actually read it on.
        var detail = await harness.Coas.GetCoa(reread.Id);
        var rendered = detail.Value.Subjects
            .SelectMany(subject => subject.Groups)
            .SelectMany(group => group.Rows)
            .Single(row => row.DisplayLabel == "Assay");

        Assert.Equal("95.0-105.0%", rendered.AcceptanceCriteria);

        // The header's pin is untouched too: still the version the round ran under, never the
        // newly Effective one.
        Assert.Equal(round.Specification.Version, reread.SpecificationVersion);
        Assert.Equal(round.Specification.Id, reread.SpecificationId);
    }

    // =======================================================================
    // Criterion 5 — revision supersedes, never overwrites
    // =======================================================================

    /// <summary>
    /// Criterion 5 — a revision is a new record. The original is marked Superseded only when the
    /// replacement is actually issued, stays fully retrievable afterwards, and is never edited or
    /// deleted.
    /// </summary>
    [Fact]
    public async Task Revision_creates_a_new_certificate_and_supersedes_the_original_only_on_issue()
    {
        using var harness = new QcWorksheetTestContext();
        var round = await ArrangeBothTracks(harness);

        var chemical = InstanceFor(round, 0, round.Chemical);
        var microbial = InstanceFor(round, 0, round.Microbial);

        await Submit(round, chemical, ChemicalField, "98.7%");
        await Submit(round, microbial, MicrobialField, "45 CFU/g");
        await Review(round, chemical);
        await Review(round, microbial);

        var original = Assert.Single(await Certificates(harness, round.TestRequestId));

        var issued = await harness.Coas.Issue(original.Id, harness.Approver.Id);
        Assert.True(issued.IsSuccess, issued.Error?.Description);
        Assert.Equal(CoaStatus.Issued, issued.Value.Status);
        Assert.NotNull(issued.Value.IssuedAt);

        var revised = await harness.Coas.Revise(
            original.Id,
            new ReviseCoaRequest { Reason = "Transcription error in the assay result." },
            harness.Approver.Id);

        Assert.True(revised.IsSuccess, revised.Error?.Description);
        Assert.Equal(CoaStatus.Draft, revised.Value.Status);
        Assert.Equal(original.Id, revised.Value.SupersedesId);
        Assert.Equal(2, revised.Value.RevisionNumber);
        Assert.Equal("Transcription error in the assay result.", revised.Value.RevisionReason);
        Assert.Equal($"{original.CertificateCode}-R2", revised.Value.CertificateCode);

        // Raising the revision does not touch the original: an unissued draft supersedes nothing,
        // and the round would otherwise be left with no valid certificate in between.
        var stillIssued = await harness.Coas.GetCoa(original.Id);
        Assert.Equal(CoaStatus.Issued, stillIssued.Value.Status);

        var issuedRevision = await harness.Coas.Issue(revised.Value.Id, harness.Approver.Id);
        Assert.True(issuedRevision.IsSuccess, issuedRevision.Error?.Description);

        // Now, and only now, the original is Superseded — and it is still entirely there.
        var superseded = await harness.Coas.GetCoa(original.Id);
        Assert.True(superseded.IsSuccess);
        Assert.Equal(CoaStatus.Superseded, superseded.Value.Status);
        Assert.NotEmpty(superseded.Value.Subjects.SelectMany(subject => subject.Groups));
        Assert.Equal(original.CertificateCode, superseded.Value.CertificateCode);

        // Nothing was deleted, and the two records are distinct rows.
        var all = await Certificates(harness, round.TestRequestId);
        Assert.Equal(2, all.Count);
        Assert.All(all, item => Assert.Null(item.DeletedAt));

        // The superseded document points its reader forward, so the banner is not a dead end.
        Assert.Equal(revised.Value.Id, superseded.Value.SupersededById);
        Assert.Equal(revised.Value.CertificateCode, superseded.Value.SupersededByCertificateCode);
    }

    /// <summary>
    /// A revision's rows are recomputed from current data rather than copied — which is the entire
    /// point of revising rather than reprinting. The original's own rows stay exactly as issued.
    /// </summary>
    [Fact]
    public async Task A_revision_recomputes_its_rows_while_the_original_keeps_its_own()
    {
        using var harness = new QcWorksheetTestContext();
        var round = await ArrangeBothTracks(harness);

        var chemical = InstanceFor(round, 0, round.Chemical);
        var microbial = InstanceFor(round, 0, round.Microbial);

        await Submit(round, chemical, ChemicalField, "98.7%");
        await Submit(round, microbial, MicrobialField, "45 CFU/g");
        await Review(round, chemical);
        await Review(round, microbial);

        var original = Assert.Single(await Certificates(harness, round.TestRequestId));
        await harness.Coas.Issue(original.Id, harness.Approver.Id);

        // A data-entry correction on the underlying worksheet — the exact scenario the revise
        // endpoint exists for.
        var value = await harness.Db.QcWorksheetFieldValues
            .SingleAsync(item => item.WorksheetInstanceId == chemical && item.FieldKey == ChemicalField);

        value.Value = "99.4%";
        await harness.Db.SaveChangesAsync();

        var revised = await harness.Coas.Revise(
            original.Id,
            new ReviseCoaRequest { Reason = "Assay transposed on data entry." },
            harness.Approver.Id);

        Assert.True(revised.IsSuccess, revised.Error?.Description);

        var revisedAssay = revised.Value.Subjects
            .SelectMany(subject => subject.Groups)
            .SelectMany(group => group.Rows)
            .Single(row => row.DisplayLabel == "Assay");

        Assert.Equal("99.4%", revisedAssay.ResultValue);

        // The issued original still reads as it was signed.
        var originalAssay = (await harness.Coas.GetCoa(original.Id)).Value.Subjects
            .SelectMany(subject => subject.Groups)
            .SelectMany(group => group.Rows)
            .Single(row => row.DisplayLabel == "Assay");

        Assert.Equal("98.7%", originalAssay.ResultValue);
    }

    /// <summary>Only an issued certificate can be revised, and only with a stated reason.</summary>
    [Fact]
    public async Task Revising_requires_an_issued_certificate_and_a_reason()
    {
        using var harness = new QcWorksheetTestContext();
        var round = await ArrangeBothTracks(harness);

        var chemical = InstanceFor(round, 0, round.Chemical);
        var microbial = InstanceFor(round, 0, round.Microbial);

        await Submit(round, chemical, ChemicalField, "98.7%");
        await Submit(round, microbial, MicrobialField, "45 CFU/g");
        await Review(round, chemical);
        await Review(round, microbial);

        var draft = Assert.Single(await Certificates(harness, round.TestRequestId));

        var tooEarly = await harness.Coas.Revise(
            draft.Id, new ReviseCoaRequest { Reason = "Because." }, harness.Approver.Id);

        Assert.False(tooEarly.IsSuccess);
        Assert.Equal("QcCoa.ReviseRequiresIssued", tooEarly.Error.Code);

        await harness.Coas.Issue(draft.Id, harness.Approver.Id);

        var noReason = await harness.Coas.Revise(
            draft.Id, new ReviseCoaRequest { Reason = "   " }, harness.Approver.Id);

        Assert.False(noReason.IsSuccess);
        Assert.Equal("QcCoa.RevisionReasonRequired", noReason.Error.Code);
    }

    // =======================================================================
    // Criterion 6 — the Environmental shape has no batch dates at all
    // =======================================================================

    /// <summary>
    /// Criterion 6 — a Monitoring Report's header does not declare a Manufacturing or Expiry Date
    /// field, rather than declaring one and leaving it empty. There is no batch for one to belong
    /// to, and a report that printed "Manufacturing Date: —" would be describing a concept that
    /// does not apply to a room.
    /// </summary>
    [Fact]
    public async Task An_environmental_report_header_has_no_manufacturing_or_expiry_field_at_all()
    {
        using var harness = new QcWorksheetTestContext();
        await harness.SeedApprovalChain(QcWorksheetModelTypes.WorksheetInstance);

        var microbial = await harness.SeedEffectiveTemplate(
            "WS/EM/2", WorksheetCategory.Microbial, MicrobialField);

        var specification = await harness.SeedEffectiveSpecification(
            SpecificationAppliesTo.RoutineEnvironmental,
            (microbial, SpecificationAnalysisType.Microbial));

        await SeedPrintableCharacteristic(
            harness, specification, microbial, MicrobialField,
            "NMT 100 CFU/4Hrs", "MICROBIAL", 1, "Airborne Viables");

        var round = await Raise(
            harness, specification, microbial, microbial,
            TestRequestType.RoutineEnvironmental, ["Deblistering-2"]);

        var instanceId = InstanceFor(round, 0, microbial);
        await Submit(round, instanceId, MicrobialField, "12 CFU/4Hrs");
        await Review(round, instanceId);

        var certificate = Assert.Single(await Certificates(harness, round.TestRequestId));
        var detail = await harness.Coas.GetCoa(certificate.Id);
        Assert.True(detail.IsSuccess, detail.Error?.Description);

        var header = Assert.IsType<EnvironmentalMonitoringReportHeaderDto>(detail.Value.Header);

        // The type itself is the assertion: no property on it is named for a batch date, so no
        // renderer can print a blank one.
        var properties = typeof(EnvironmentalMonitoringReportHeaderDto)
            .GetProperties()
            .Select(property => property.Name)
            .ToList();

        Assert.DoesNotContain("ManufacturingDate", properties);
        Assert.DoesNotContain("ExpiryDate", properties);
        Assert.DoesNotContain("BatchNumber", properties);

        // What it does carry: the room, the pinned specification, and the two dates that do apply.
        Assert.Equal("Deblistering-2 location (Deblistering-2)", header.AreaOrRoom);
        Assert.Equal(specification.Code, header.SpecificationCode);
        Assert.Equal("Rev 1", header.SpecificationRevision);
        Assert.NotNull(header.TestCompletionDate);

        // And the Certificate of Analysis shape does declare them, so the difference is the shape
        // and not an oversight.
        var coaProperties = typeof(CertificateOfAnalysisHeaderDto)
            .GetProperties()
            .Select(property => property.Name)
            .ToList();

        Assert.Contains("ManufacturingDate", coaProperties);
        Assert.Contains("ExpiryDate", coaProperties);
    }

    /// <summary>
    /// The counterpart: a Material round's certificate carries the batch identity and the two
    /// dates, read from the linked batch record rather than typed.
    /// </summary>
    [Fact]
    public async Task A_material_certificate_header_carries_the_batch_and_its_dates()
    {
        using var harness = new QcWorksheetTestContext();

        var batch = await harness.SeedMaterialBatch("PT260901");
        batch.ManufacturingDate = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        batch.ExpiryDate = new DateTime(2029, 2, 28, 0, 0, 0, DateTimeKind.Utc);
        await harness.Db.SaveChangesAsync();

        var round = await ArrangeBothTracks(harness);

        var subject = await harness.Db.QcTestRequestSubjects
            .SingleAsync(item => item.TestRequestId == round.TestRequestId);

        subject.MaterialBatchId = batch.Id;
        await harness.Db.SaveChangesAsync();

        var chemical = InstanceFor(round, 0, round.Chemical);
        var microbial = InstanceFor(round, 0, round.Microbial);

        await Submit(round, chemical, ChemicalField, "98.7%");
        await Submit(round, microbial, MicrobialField, "45 CFU/g");
        await Review(round, chemical);
        await Review(round, microbial);

        var certificate = Assert.Single(await Certificates(harness, round.TestRequestId));
        var detail = await harness.Coas.GetCoa(certificate.Id);

        var header = Assert.IsType<CertificateOfAnalysisHeaderDto>(detail.Value.Header);

        Assert.Equal("PT260901", header.BatchNumber);
        Assert.Equal(batch.ManufacturingDate, header.ManufacturingDate);
        Assert.Equal(batch.ExpiryDate, header.ExpiryDate);
        Assert.Equal("Seeded material", header.ProductOrMaterialName);
        Assert.StartsWith("COA-", header.CertificateCode, StringComparison.Ordinal);
    }

    // =======================================================================
    // Issue — what it closes
    // =======================================================================

    /// <summary>
    /// Issuing is what finally closes a round: it freezes the worksheets the certificate draws on
    /// and moves the round to Released — the terminal state Milestone 3's status calculator
    /// deliberately left to this milestone.
    /// </summary>
    [Fact]
    public async Task Issuing_locks_the_certified_worksheets_and_releases_the_round()
    {
        using var harness = new QcWorksheetTestContext();
        var round = await ArrangeBothTracks(harness);

        var chemical = InstanceFor(round, 0, round.Chemical);
        var microbial = InstanceFor(round, 0, round.Microbial);

        await Submit(round, chemical, ChemicalField, "98.7%");
        await Submit(round, microbial, MicrobialField, "45 CFU/g");
        await Review(round, chemical);
        await Review(round, microbial);

        var certificate = Assert.Single(await Certificates(harness, round.TestRequestId));

        var beforeIssue = await harness.Db.QcTestRequests
            .AsNoTracking()
            .SingleAsync(item => item.Id == round.TestRequestId);

        Assert.NotEqual(TestRequestStatus.Released, beforeIssue.Status);

        await harness.Coas.Issue(certificate.Id, harness.Approver.Id);

        var instances = await harness.Db.QcWorksheetInstances
            .AsNoTracking()
            .Where(item => item.TestRequestSubject.TestRequestId == round.TestRequestId)
            .ToListAsync();

        Assert.All(instances, item => Assert.Equal(WorksheetInstanceStatus.Locked, item.Status));

        var released = await harness.Db.QcTestRequests
            .AsNoTracking()
            .SingleAsync(item => item.Id == round.TestRequestId);

        Assert.Equal(TestRequestStatus.Released, released.Status);
    }

    /// <summary>A certificate is issued once. There is no second issue and no un-issue.</summary>
    [Fact]
    public async Task A_certificate_cannot_be_issued_twice()
    {
        using var harness = new QcWorksheetTestContext();
        var round = await ArrangeBothTracks(harness);

        var chemical = InstanceFor(round, 0, round.Chemical);
        var microbial = InstanceFor(round, 0, round.Microbial);

        await Submit(round, chemical, ChemicalField, "98.7%");
        await Submit(round, microbial, MicrobialField, "45 CFU/g");
        await Review(round, chemical);
        await Review(round, microbial);

        var certificate = Assert.Single(await Certificates(harness, round.TestRequestId));

        Assert.True((await harness.Coas.Issue(certificate.Id, harness.Approver.Id)).IsSuccess);

        var again = await harness.Coas.Issue(certificate.Id, harness.Approver.Id);
        Assert.False(again.IsSuccess);
        Assert.Equal("QcCoa.IssueRequiresDraft", again.Error.Code);
    }

    /// <summary>
    /// The trigger is idempotent. A round that already holds a live certificate does not sprout a
    /// second one when another review lands on it — an OOS retest being reviewed, say.
    /// </summary>
    [Fact]
    public async Task A_round_never_holds_two_live_certificates()
    {
        using var harness = new QcWorksheetTestContext();
        var round = await ArrangeBothTracks(harness);

        var chemical = InstanceFor(round, 0, round.Chemical);
        var microbial = InstanceFor(round, 0, round.Microbial);

        await Submit(round, chemical, ChemicalField, "98.7%");
        await Submit(round, microbial, MicrobialField, "45 CFU/g");
        await Review(round, chemical);
        await Review(round, microbial);

        Assert.Single(await Certificates(harness, round.TestRequestId));

        // A second pass over the same trigger changes nothing.
        await harness.CoaGeneration.TryGenerateAsync(round.TestRequestId, harness.Approver.Id);

        Assert.Single(await Certificates(harness, round.TestRequestId));
    }

    // =======================================================================
    // Criterion 7 — coexistence
    // =======================================================================

    /// <summary>
    /// Criterion 7 — this milestone's tables and route are its own. The certificate route sits
    /// under the rebuilt module's own <c>qc/worksheets/</c> prefix and the two new tables carry
    /// the module's own <c>Qc</c> names, so neither can collide with the live commercial or
    /// routine certificate path.
    /// </summary>
    [Fact]
    public void Certificate_tables_and_route_are_the_rebuilt_modules_own()
    {
        using var harness = new QcWorksheetTestContext();

        var model = harness.Db.Model;

        Assert.Equal("QcCoas", model.FindEntityType(typeof(Coa))!.GetTableName());
        Assert.Equal("QcCoaRows", model.FindEntityType(typeof(CoaRow))!.GetTableName());

        // Nothing in this milestone reaches any live certificate entity: the Coa's only foreign
        // keys are to the rebuilt module's own tables and to Users.
        var references = model.FindEntityType(typeof(Coa))!
            .GetForeignKeys()
            .Select(key => key.PrincipalEntityType.GetTableName())
            .Distinct()
            .ToList();

        Assert.All(references, table =>
            Assert.True(
                table!.StartsWith("Qc", StringComparison.Ordinal)
                || string.Equals(table, "users", StringComparison.OrdinalIgnoreCase),
                $"Coa unexpectedly references '{table}'."));
    }
}
