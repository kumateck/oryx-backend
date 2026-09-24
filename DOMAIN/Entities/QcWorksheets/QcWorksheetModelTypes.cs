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

    public static bool IsQcWorksheetModelType(string modelType) =>
        modelType is StandardTestProcedure or WorksheetTemplate or Specification;
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

    /// <summary>
    /// Maps an approval-engine modelType to the EntityType recorded on the QcApproval row.
    /// </summary>
    public static string FromModelType(string modelType) => modelType switch
    {
        QcWorksheetModelTypes.StandardTestProcedure => StandardTestProcedure,
        QcWorksheetModelTypes.WorksheetTemplate => WorksheetTemplate,
        QcWorksheetModelTypes.Specification => Specification,
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
