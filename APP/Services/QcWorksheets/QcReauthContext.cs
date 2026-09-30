namespace APP.Services.QcWorksheets;

/// <summary>
/// A request-scoped marker proving that the acting user re-authenticated during this
/// request.
/// <para>
/// This is what closes the one real gap in the generic approval engine: its
/// <c>ApproveItem</c>/<c>RejectItem</c> endpoints require only a valid session, with no
/// re-authentication. QC was locked to full meaning-of-signature, a deliberately stricter
/// bar than the rest of the company.
/// </para>
/// <para>
/// The QC re-auth wrapper calls <see cref="Confirm"/> only after verifying the caller's own
/// password, and then calls the real <c>IApprovalRepository.ApproveItem</c>. The QC approval
/// handler refuses to act unless this has been confirmed — so hitting the <i>generic</i>
/// approval endpoint with a QC modelType cannot approve a QC document.
/// </para>
/// </summary>
public interface IQcReauthContext
{
    /// <summary>When the acting user re-authenticated during this request, if they did.</summary>
    DateTime? ConfirmedAt { get; }

    /// <summary>The user who re-authenticated, used to reject a mismatched actor.</summary>
    Guid? ConfirmedUserId { get; }

    void Confirm(Guid userId);
}

/// <summary>
/// Scoped implementation: one instance per HTTP request, so a confirmation can never leak
/// across requests or users.
/// </summary>
public class QcReauthContext : IQcReauthContext
{
    public DateTime? ConfirmedAt { get; private set; }

    public Guid? ConfirmedUserId { get; private set; }

    public void Confirm(Guid userId)
    {
        ConfirmedUserId = userId;
        ConfirmedAt = DateTime.UtcNow;
    }
}
