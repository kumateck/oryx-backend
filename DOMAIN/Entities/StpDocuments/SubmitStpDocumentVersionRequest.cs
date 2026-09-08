namespace DOMAIN.Entities.StpDocuments;

/// <summary>
/// Body for the review/approve/reject transitions. Password is re-verified server-side
/// immediately before the transition commits (21 CFR 11.200 - a real e-signature needs a
/// signing component only the signer holds, executed at the moment of signing, not just
/// "already logged in"). Meaning is the free-text e-signature meaning (21 CFR 11.50), e.g.
/// "Approved as the effective STP" / "Reviewed" / "Rejected - missing acceptance criteria".
/// </summary>
public class SubmitStpDocumentVersionRequest
{
    public string Password { get; set; }

    public string Meaning { get; set; }
}

/// <summary>
/// Body for starting a new draft (upload/new) when the STP document is currently Approved.
/// ReasonForChange is required only in that case - best-practice document control expects
/// each revision of an already-approved document to record why it's being revised.
/// </summary>
public class StartStpDraftRequest
{
    public string ReasonForChange { get; set; }
}
