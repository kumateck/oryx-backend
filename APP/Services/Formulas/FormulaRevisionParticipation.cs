namespace APP.Services.Formulas;

/// <summary>Any two formula revision roles may overlap; all three may not.</summary>
public static class FormulaRevisionParticipation
{
    public static bool AllowsApproval(Guid? authorId, Guid? reviewerId, Guid actorId) =>
        authorId.HasValue && authorId != Guid.Empty &&
        reviewerId.HasValue && reviewerId != Guid.Empty && actorId != Guid.Empty &&
        !(authorId == actorId && reviewerId == actorId);
}
