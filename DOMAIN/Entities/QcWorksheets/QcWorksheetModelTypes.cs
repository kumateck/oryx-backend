namespace DOMAIN.Entities.QcWorksheets;

/// <summary>
/// The <c>modelType</c> strings this module registers with the existing generic
/// Approval engine (<see cref="DOMAIN.Entities.Approvals.Approval"/>).
/// An administrator configures the approval chain for each of these through the
/// existing generic Approval/ApprovalStage configuration — QC does not build its own
/// chain-configuration UI.
/// </summary>
public static class QcWorksheetModelTypes
{
    public const string StandardTestProcedure = "QcStandardTestProcedure";
    public const string WorksheetTemplate = "QcWorksheetTemplate";
    public const string Specification = "QcSpecification";

    /// <summary>
    /// Milestone 3. A submitted worksheet's review is an approval like any other QC approval:
    /// same engine, same centralized table, same re-authentication wrapper. Reassignment is
    /// deliberately not here — it is an administrative action, not a signature.
    /// </summary>
    public const string WorksheetInstance = "QcWorksheetInstance";

    /// <summary>
    /// Milestone 4. The QA disposition of an OOS case is an approval like every other QC
    /// approval: same engine, same centralized <see cref="QcApproval"/> table, same
    /// re-authentication wrapper. There is deliberately no separate OOS-only signature
    /// mechanism — an earlier draft of this module was already corrected once for inventing a
    /// parallel audit table, and that correction holds here.
    /// </summary>
    public const string OosCase = "QcOosCase";

    public static bool IsQcWorksheetModelType(string modelType) =>
        modelType is StandardTestProcedure or WorksheetTemplate or Specification
            or WorksheetInstance or OosCase;
}

/// <summary>
/// The value stored in <see cref="QcApproval.EntityType"/>. Deliberately a free string
/// rather than a closed enum so a later milestone (Specification, WorksheetInstance,
/// OosCase) never needs a migration to add a case.
/// </summary>
public static class QcApprovalEntityTypes
{
    public const string StandardTestProcedure = "StandardTestProcedure";
    public const string WorksheetTemplate = "WorksheetTemplate";
    public const string Specification = "Specification";
    public const string WorksheetInstance = "WorksheetInstance";
    public const string OosCase = "OosCase";

    /// <summary>
    /// Maps an approval-engine modelType to the EntityType recorded on the QcApproval row.
    /// </summary>
    public static string FromModelType(string modelType) => modelType switch
    {
        QcWorksheetModelTypes.StandardTestProcedure => StandardTestProcedure,
        QcWorksheetModelTypes.WorksheetTemplate => WorksheetTemplate,
        QcWorksheetModelTypes.Specification => Specification,
        QcWorksheetModelTypes.WorksheetInstance => WorksheetInstance,
        QcWorksheetModelTypes.OosCase => OosCase,
        _ => null
    };
}

/// <summary>
/// The Draft -> UnderReview -> Approved -> Effective -> Superseded lifecycle shared by
/// every controlled QC document. Deliberately <b>not</b> <c>IVerifiable</c>, which is a
/// single-flag pattern and cannot express these five states.
/// </summary>
public enum QcDocumentStatus
{
    Draft = 0,
    UnderReview = 1,
    Approved = 2,
    Effective = 3,
    Superseded = 4
}
