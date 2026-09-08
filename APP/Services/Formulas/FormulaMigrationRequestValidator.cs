namespace APP.Services.Formulas;

internal static class FormulaMigrationRequestValidator
{
    public static void Validate(FormulaMigrationDryRunRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ReleaseId))
            throw new ArgumentException("A migration release ID is required.");
        if (request.ReleaseId.Length > 100)
            throw new ArgumentException("Migration release ID cannot exceed 100 characters.");
        if (string.IsNullOrWhiteSpace(request.CodeVersion))
            throw new ArgumentException("A migration code version is required.");
        if (request.CodeVersion.Length > 100)
            throw new ArgumentException("Migration code version cannot exceed 100 characters.");
        if (!FormulaMigrationHashing.IsSha256(request.CorpusChecksum))
            throw new ArgumentException("Corpus checksum must be a lowercase SHA-256.");
        if (request.Decisions is null)
            throw new ArgumentException("Migration decisions are required.");
        foreach (var decision in request.Decisions)
        {
            if (string.IsNullOrWhiteSpace(decision.LegacyPath) ||
                decision.LegacyPath.Length > 500)
                throw new ArgumentException(
                    "Decision legacy path must contain 1 to 500 characters.");
            if (decision.Action?.Length > 100)
                throw new ArgumentException(
                    "Decision action cannot exceed 100 characters.");
            if (decision.ApprovalReference?.Length > 250)
                throw new ArgumentException(
                    "Decision approval reference cannot exceed 250 characters.");
            if (decision.ApprovalScope.HasValue &&
                !Enum.IsDefined(decision.ApprovalScope.Value))
                throw new ArgumentException("Decision approval scope is invalid.");
        }
    }
}
