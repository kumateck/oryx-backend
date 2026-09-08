namespace APP.Services.Formulas;

public sealed record FormulaMigrationEvidenceReceipt(
    Guid RunId,
    bool AlreadyRecorded,
    FormulaMigrationDryRunReport Report
);

public interface IFormulaMigrationEvidenceService
{
    Task<FormulaMigrationEvidenceReceipt> RecordDryRunAsync(
        FormulaMigrationDryRunRequest request,
        CancellationToken cancellationToken = default
    );
}
