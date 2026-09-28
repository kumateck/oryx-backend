using DOMAIN.Entities.QcWorksheets;
using SHARED;

namespace APP.Services.QcWorksheets.WorksheetDocxImport.AiExtraction;

/// <summary>
/// Build brief 10's fallback extraction path. Called only when classification produces no
/// recognizer (<see cref="ArdFamily.Unknown"/>, not a Standard Test Procedure) and the caller
/// holds <see cref="Utils.QcWorksheetPermissionKeys.CanUseAiWorksheetExtraction"/>. Never called
/// for a file that matches a built family.
/// </summary>
public interface IAiWorksheetExtractor
{
    Task<Result<AiExtractionResult>> ExtractAsync(
        RedactedDocument document, CancellationToken cancellationToken);
}

public sealed record AiExtractionResult(
    ProposedWorksheetTemplate Template,
    List<SpecificationCharacteristicProposal> SpecificationProposals,
    List<WorksheetImportFlag> Flags,
    int InputTokens,
    int OutputTokens);
