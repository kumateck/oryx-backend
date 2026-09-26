namespace APP.Utils;

/// <summary>
/// Permission keys for the rebuilt QC module. One key per state-machine transition,
/// following the project's convention that every action/view gets its own dedicated key
/// and never shares one.
/// <para>
/// These are additive: they neither replace nor overlap the live Material/Product/
/// Packaging QC keys in <see cref="PermissionKeys"/>.
/// </para>
/// </summary>
public static class QcWorksheetPermissionKeys
{
    // Standard Test Procedures
    public const string CanViewQcStps = "CanViewQcStps";
    public const string CanCreateQcStp = "CanCreateQcStp";
    public const string CanEditQcStp = "CanEditQcStp";
    public const string CanApproveQcStp = "CanApproveQcStp";
    public const string CanSupersedeQcStp = "CanSupersedeQcStp";

    /// <summary>
    /// Deliberately separate from <see cref="CanCreateQcStp"/>: bulk-importing the existing
    /// STP library is a migration-scale activity worth restricting independently of
    /// everyday STP authoring.
    /// </summary>
    public const string CanImportQcStp = "CanImportQcStp";

    // Worksheet Templates
    public const string CanViewWorksheetTemplates = "CanViewWorksheetTemplates";
    public const string CanCreateWorksheetTemplate = "CanCreateWorksheetTemplate";
    public const string CanEditWorksheetTemplate = "CanEditWorksheetTemplate";
    public const string CanApproveWorksheetTemplate = "CanApproveWorksheetTemplate";
    public const string CanSupersedeWorksheetTemplate = "CanSupersedeWorksheetTemplate";

    /// <summary>
    /// Deliberately separate from <see cref="CanCreateWorksheetTemplate"/>, mirroring
    /// <see cref="CanImportQcStp"/>: turning the lab's ARD Word worksheets into proposals is a
    /// migration-scale activity worth restricting independently. The import itself writes
    /// nothing; saving a proposal still goes through the create endpoints and their keys.
    /// </summary>
    public const string CanImportQcWorksheetTemplates = "CanImportQcWorksheetTemplates";

    // Specifications
    public const string CanViewQcSpecifications = "CanViewQcSpecifications";
    public const string CanCreateQcSpecification = "CanCreateQcSpecification";
    public const string CanEditQcSpecification = "CanEditQcSpecification";
    public const string CanApproveQcSpecification = "CanApproveQcSpecification";
    public const string CanSupersedeQcSpecification = "CanSupersedeQcSpecification";

    // Sampling Point Groups

    /// <summary>
    /// Read access to the sampling point group list, separate from
    /// <see cref="CanManageSamplingPointGroups"/>: a role with Specification-authoring rights
    /// but no reference-data management rights still has to list groups to populate the
    /// dropdown a characteristic's Alert/Action tier is grouped by. View is a genuinely
    /// distinct concern from Manage — many roles legitimately need read access to reference
    /// data without deserving admin rights over it.
    /// </summary>
    public const string CanViewSamplingPointGroups = "CanViewSamplingPointGroups";

    /// <summary>
    /// Create/edit/delete, as a single key rather than the create/edit/approve/supersede split
    /// the controlled documents get: a sampling point group is plain reference data maintained
    /// by the QC Manager, with no lifecycle of its own whose transitions could be gated
    /// separately. Only View splits off, and only because it is a different concern, not a
    /// different lifecycle stage.
    /// </summary>
    public const string CanManageSamplingPointGroups = "CanManageSamplingPointGroups";

    // Sampling Points

    /// <summary>
    /// Read access to the sampling point register, separate from
    /// <see cref="CanManageSamplingPoints"/> for the same reason
    /// <see cref="CanViewSamplingPointGroups"/> is separate from its Manage key: many roles
    /// legitimately need to read reference data — to pick a point when authoring a monitoring
    /// program, for one — without deserving admin rights over the register itself.
    /// </summary>
    public const string CanViewSamplingPoints = "CanViewSamplingPoints";

    /// <summary>
    /// Sampling point master data (Milestone 6), on its own dedicated key rather than sharing the
    /// monitoring program keys. Every action and view in this codebase gets its own key and never
    /// shares one, and a sampling point is a different entity from the schedule that references
    /// it: someone may legitimately maintain the point register without also being able to decide
    /// how often anything gets tested.
    /// <para>
    /// Create/edit/delete as one key rather than a create/edit split, mirroring
    /// <see cref="CanManageSamplingPointGroups"/> — this entity's closest sibling in the module,
    /// and the precedent for simple master data here. Both are plain reference data with no
    /// lifecycle of their own whose transitions could be gated separately; the multi-way splits in
    /// this file are reserved for things that have real transitions to gate. Only View splits off,
    /// and only because reading is a different concern from administering, not a different
    /// lifecycle stage.
    /// </para>
    /// </summary>
    public const string CanManageSamplingPoints = "CanManageSamplingPoints";

    // Test requests (the ARD / round)
    public const string CanViewQcTestRequests = "CanViewQcTestRequests";

    /// <summary>
    /// Split from <see cref="CanCreateUnscheduledQcTestRequest"/> because the two are different
    /// authorities: a scheduled round is normally system-raised and this key only covers a
    /// manual override of one, while raising a round outside the schedule always demands a
    /// stated reason.
    /// </summary>
    public const string CanCreateScheduledQcTestRequest = "CanCreateScheduledQcTestRequest";

    public const string CanCreateUnscheduledQcTestRequest = "CanCreateUnscheduledQcTestRequest";
    public const string CanRecordQcSample = "CanRecordQcSample";
    public const string CanAssignQcTestRequest = "CanAssignQcTestRequest";

    // Test room — assignment
    public const string CanAssignWorksheet = "CanAssignWorksheet";

    /// <summary>
    /// Separate from <see cref="CanAssignWorksheet"/>: taking work off an analyst who may
    /// already have entered GxP-relevant data is a different decision from handing out work
    /// that nobody has started.
    /// </summary>
    public const string CanReassignWorksheet = "CanReassignWorksheet";

    // Test room — execution, split by analysis track. Chemical and Microbial analysts are
    // different people in different rooms, so each transition is grantable per track.
    public const string CanStartChemicalWorksheet = "CanStartChemicalWorksheet";
    public const string CanStartMicrobialWorksheet = "CanStartMicrobialWorksheet";
    public const string CanEnterChemicalWorksheetResult = "CanEnterChemicalWorksheetResult";
    public const string CanEnterMicrobialWorksheetResult = "CanEnterMicrobialWorksheetResult";
    public const string CanSubmitChemicalWorksheet = "CanSubmitChemicalWorksheet";
    public const string CanSubmitMicrobialWorksheet = "CanSubmitMicrobialWorksheet";
    public const string CanReviewChemicalWorksheet = "CanReviewChemicalWorksheet";
    public const string CanReviewMicrobialWorksheet = "CanReviewMicrobialWorksheet";
    public const string CanReturnWorksheetForCorrection = "CanReturnWorksheetForCorrection";

    // OOS cases. Three keys for three genuinely different authorities: running the Phase 1
    // lab-error check, deciding a retest is warranted, and signing the QA disposition that
    // rejects or releases a real batch. In a real lab these sit with a QC Officer, a QC
    // Manager and a QA Manager respectively, which is exactly why they are not one key.

    /// <summary>
    /// Read access to the OOS queue and to a single case, separate from
    /// <see cref="CanInvestigateQcOosCase"/> for the same reason
    /// <see cref="CanViewSamplingPointGroups"/> is separate from its Manage key: reading is a
    /// different concern from acting.
    /// <para>
    /// The concrete failure this fixes is a QA Manager holding only
    /// <see cref="CanDispositionQcOosCase"/> — the person who must sign the disposition —
    /// being unable to load the case they are signing. The QC Manager holding only
    /// <see cref="CanAuthorizeQcOosRetest"/> was locked out the same way. Both authorities act
    /// on a case they could not read, which is not a workable split.
    /// </para>
    /// </summary>
    public const string CanViewQcOosCases = "CanViewQcOosCases";

    public const string CanInvestigateQcOosCase = "CanInvestigateQcOosCase";
    public const string CanAuthorizeQcOosRetest = "CanAuthorizeQcOosRetest";
    public const string CanDispositionQcOosCase = "CanDispositionQcOosCase";

    // Certificates. Named by docs/qc-rebuild/build-briefs/05-certificates.md and allocated to
    // roles by docs/qc-rebuild/roles-permission-matrix.md, which grants view to QA Executive and
    // upward but keeps issue and revise with QA Manager/Deputy/Head only. Viewing a certificate
    // and putting one's name on it are plainly different authorities, so they are separate keys —
    // and issuing an original is separated from reissuing one that is already in circulation,
    // since a revision withdraws a document other parties may already be relying on.
    //
    // There is deliberately no "create" key: a certificate is generated by the system when the
    // round it certifies passes the strict-hold gate, never authored by a person.
    public const string CanViewQcCertificate = "CanViewQcCertificate";
    public const string CanIssueQcCertificate = "CanIssueQcCertificate";
    public const string CanReviseQcCertificate = "CanReviseQcCertificate";

    // Monitoring programs — the scheduling configuration behind routine Water/Environmental
    // testing. Four keys, exactly as named by docs/qc-rebuild/permissions.md; a monitoring
    // program has no approval lifecycle, being operational configuration rather than a
    // controlled document, so there is nothing further to gate.
    //
    // SamplingPoint master data sits behind these same four keys rather than a fifth of its own:
    // the brief for this milestone states its key list is already defined and adds none, and a
    // point exists only to be scheduled by a program. Creating a point and creating the program
    // that schedules it are the same act of configuration by the same person.
    public const string CanViewMonitoringPrograms = "CanViewMonitoringPrograms";
    public const string CanCreateMonitoringProgram = "CanCreateMonitoringProgram";
    public const string CanEditMonitoringProgram = "CanEditMonitoringProgram";

    /// <summary>
    /// Covers Resume as well as Pause. The two are one authority over whether a schedule runs,
    /// not two — and unlike every other paired transition in this module, neither direction
    /// writes a result, a signature or a batch status.
    /// </summary>
    public const string CanPauseMonitoringProgram = "CanPauseMonitoringProgram";

    // Water quality. Viewing a validity window, telling production it may rely on one, and
    // withdrawing one are three genuinely different authorities, so they are three keys —
    // and recording a use is a fourth, held by production-facing staff who activate nothing.
    public const string CanViewQcWaterQualityPeriods = "CanViewQcWaterQualityPeriods";
    public const string CanActivateQcWaterQualityPeriod = "CanActivateQcWaterQualityPeriod";
    public const string CanHoldQcWaterQualityPeriod = "CanHoldQcWaterQualityPeriod";
    public const string CanRecordQcWaterUse = "CanRecordQcWaterUse";

    // Approval audit. The self-scoped "my pending" queue intentionally needs no view key,
    // but the full signature trail accepts an arbitrary entity type and id and therefore must
    // be an independently grantable view rather than being open to every authenticated user.
    public const string CanViewQcApprovalHistory = "CanViewQcApprovalHistory";
}
