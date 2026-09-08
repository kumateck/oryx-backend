using System.Security.Cryptography;

#nullable enable

namespace APP.Services.Formulas;

public static class FormulaMigrationPackage
{
    public const long SignedReportByteLimit = 64L * 1024 * 1024;

    public static FormulaMigrationApplyRequest Seal(
        FormulaMigrationApplyDraft draft,
        string signedReportLocation,
        string signedReportHash)
    {
        ArgumentNullException.ThrowIfNull(draft);
        var unsigned = new FormulaMigrationApplyRequest(
            draft.DryRunId,
            signedReportLocation,
            signedReportHash,
            new string('0', 64),
            draft.Targets,
            draft.DecisionEvidence);
        var request = unsigned with
        {
            ManifestHash = FormulaMigrationApplyManifest.ComputeHash(unsigned)
        };
        FormulaMigrationApplyValidation.ValidateOffline(request);
        return request;
    }

    public static FormulaMigrationPackageValidationReceipt ValidateOffline(
        FormulaMigrationApplyRequest request,
        string? observedSignedReportHash = null)
    {
        var receipt = FormulaMigrationApplyValidation.ValidateOffline(request);
        if (observedSignedReportHash is not null &&
            !CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(request.SignedReportHash),
                Convert.FromHexString(observedSignedReportHash)))
            throw new InvalidOperationException(
                "Signed approval report content does not match the sealed package.");
        return receipt;
    }
}
