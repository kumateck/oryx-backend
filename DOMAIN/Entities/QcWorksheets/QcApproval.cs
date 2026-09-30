using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Approvals;

namespace DOMAIN.Entities.QcWorksheets;

/// <summary>
/// The single, centralized approval/e-signature table covering every QC approval point.
/// <para>
/// This subclasses the existing <see cref="ResponsibleApprovalStage"/> exactly as the
/// Form module's <c>ResponseApproval</c> does, and the stage rows are created up front by
/// the existing <c>IApprovalRepository.CreateInitialApprovalsAsync</c> machinery. The one
/// deliberate divergence from the Form module's convention is that this is <b>one table
/// for all QC entities</b> rather than one table per entity, so that QC approvals are
/// manageable as a single queue.
/// </para>
/// <para>
/// Rows are addressed by (<see cref="EntityType"/>, <see cref="EntityId"/>) rather than by
/// a typed navigation property, because they span multiple unrelated tables.
/// </para>
/// </summary>
public class QcApproval : ResponsibleApprovalStage
{
    public Guid Id { get; set; }

    /// <summary>
    /// "StandardTestProcedure" | "WorksheetTemplate" | "Specification" |
    /// "WorksheetInstance" | "OosCase". Free string, not a closed enum, so later
    /// milestones add cases without a migration. See <see cref="QcApprovalEntityTypes"/>.
    /// </summary>
    [StringLength(100)]
    public string EntityType { get; set; }

    /// <summary>
    /// The approved entity's Id. Intentionally carries no FK constraint, since it spans
    /// multiple tables; query by (EntityType, EntityId).
    /// </summary>
    public Guid EntityId { get; set; }

    /// <summary>FK to the Approval chain definition this stage row was created from.</summary>
    public Guid ApprovalId { get; set; }

    public Approval Approval { get; set; }

    /// <summary>Mirrors <c>ResponseApproval.ApprovalRound</c>.</summary>
    public int ApprovalRound { get; set; } = 1;

    /// <summary>
    /// The QC-specific addition beyond the base class: set only after the re-auth wrapper
    /// has verified the acting user's own credentials. Never client-supplied.
    /// <para>
    /// Nullable in storage because stage rows are created at submit-for-review time,
    /// before anyone has re-authenticated — the same lifecycle <c>ResponseApproval</c> rows
    /// have. The invariant <c>Status != Pending =&gt; ReauthConfirmedAt != null</c> is
    /// enforced in <c>QcApprovalHandler</c>, which is the only writer of this column.
    /// </para>
    /// </summary>
    public DateTime? ReauthConfirmedAt { get; set; }
}
