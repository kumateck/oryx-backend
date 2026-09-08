namespace APP.Services.Formulas;

internal static partial class FormulaMigrationApplyValidation
{
    public static FormulaMigrationPackageValidationReceipt ValidateOffline(
        FormulaMigrationApplyRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateRequestEnvelope(request);
        var targets = ValidateTargets(request.Targets, null);
        var evidence = ValidateEvidence(request.DecisionEvidence);
        return new FormulaMigrationPackageValidationReceipt(
            request.ManifestHash,
            targets.Count,
            evidence.Count,
            evidence.Values.Sum(item => item.KeyMappings?.Count ?? 0));
    }
}
