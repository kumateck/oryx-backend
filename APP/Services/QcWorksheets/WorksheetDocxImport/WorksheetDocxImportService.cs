using APP.Services.QcWorksheets.WorksheetDocxImport.AiExtraction;
using DOMAIN.Entities.QcWorksheets;
using Microsoft.AspNetCore.Http;

namespace APP.Services.QcWorksheets.WorksheetDocxImport;

public interface IWorksheetDocxImportService
{
    /// <summary>
    /// One proposal per file. Writes nothing. <paramref name="allowAiExtraction"/> is the
    /// caller's <c>CanUseAiWorksheetExtraction</c> permission (build brief 10); without it, an
    /// otherwise-unrecognized ARD still gets the plain <c>UnknownFamily</c> refusal, unchanged
    /// from brief 07.
    /// </summary>
    Task<List<WorksheetImportProposal>> ProposeAsync(
        IEnumerable<IFormFile> files, bool allowAiExtraction = false, CancellationToken cancellationToken = default);
}

/// <summary>
/// Turns ARD worksheets into reviewable proposals (brief 07, Phase B): read → classify →
/// family recognizer. Certificates and files that classify to a genuinely different document
/// kind (Standard Test Procedures) stop at classification with a flag; families whose
/// recognizer is not built yet stop with <see cref="WorksheetImportFlagCodes.RecognizerPending"/>.
/// An ARD-shaped document that matches no built family falls back to <see cref="IAiWorksheetExtractor"/>
/// (build brief 10) when the caller holds <c>CanUseAiWorksheetExtraction</c>; a file that
/// matches a built family never reaches the AI extractor, even when that permission is held.
/// Nothing is persisted and nothing is approved automatically.
/// </summary>
public class WorksheetDocxImportService(
    IWorksheetImportCatalogLoader catalogLoader,
    IAiWorksheetExtractor aiExtractor) : IWorksheetDocxImportService
{
    private static readonly IReadOnlyDictionary<ArdFamily, IArdFamilyRecognizer> Recognizers =
        new IArdFamilyRecognizer[]
        {
            new CultureMediaRecognizer(), new ProductMicroRecognizer(), new PurifiedWaterRecognizer(), new EnvironmentalMonitoringRecognizer(),
            new RawMaterialChemicalRecognizer(), new RawMaterialSpecificationRecognizer()
        }.ToDictionary(recognizer => recognizer.Family);

    public async Task<List<WorksheetImportProposal>> ProposeAsync(
        IEnumerable<IFormFile> files, bool allowAiExtraction = false, CancellationToken cancellationToken = default)
    {
        var catalog = await catalogLoader.LoadAsync(cancellationToken);
        var proposals = new List<WorksheetImportProposal>();

        foreach (var file in files)
        {
            var refusal = DocxUploadGuard.Validate(file);
            if (refusal is not null)
            {
                proposals.Add(Refused(file?.FileName, refusal));
                continue;
            }

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream, cancellationToken);
            stream.Position = 0;
            var proposal = Propose(file.FileName, stream, catalog);

            if (allowAiExtraction && IsAiFallbackCandidate(proposal))
            {
                stream.Position = 0;
                proposal = await ProposeWithAiFallback(file.FileName, stream, proposal, cancellationToken);
            }

            proposals.Add(proposal);
        }

        ApplyBatchRules(proposals, catalog);
        return proposals;
    }

    /// <summary>
    /// True only for the "otherwise" case in brief 10: <see cref="ArdFamily.Unknown"/> whose
    /// evidence is not "STANDARD TEST PROCEDURE" (a different document kind entirely, where AI
    /// extraction would be the wrong tool — that refusal stays exactly as-is). A file that
    /// matched a built family already has a Template, or a RecognizerPending/CompletedOutputNotTemplate
    /// flag, and is never a candidate.
    /// </summary>
    private static bool IsAiFallbackCandidate(WorksheetImportProposal proposal) =>
        proposal.Family == ArdFamily.Unknown &&
        proposal.Flags.Any(flag =>
            flag.Code == WorksheetImportFlagCodes.UnknownFamily &&
            !flag.Message.Contains("Standard Test Procedure", StringComparison.OrdinalIgnoreCase));

    private async Task<WorksheetImportProposal> ProposeWithAiFallback(
        string fileName, Stream stream, WorksheetImportProposal fallbackProposal, CancellationToken cancellationToken)
    {
        DocxDocument document;
        try
        {
            document = DocxDocumentReader.Read(stream);
        }
        catch
        {
            // Already refused by the deterministic path above; nothing further to try.
            return fallbackProposal;
        }

        var redacted = DocumentRedactor.Redact(document);
        if (redacted.IsFailure)
        {
            Flag(fallbackProposal, WorksheetImportFlagCodes.RedactionRefused,
                "A run-data label was found but its value could not be confidently redacted, so this document was not sent for AI extraction.");
            return fallbackProposal;
        }

        var extraction = await aiExtractor.ExtractAsync(redacted.Value, cancellationToken);
        if (extraction.IsFailure)
        {
            Flag(fallbackProposal, extraction.Error.Code, extraction.Error.Description);
            return fallbackProposal;
        }

        var proposal = new WorksheetImportProposal
        {
            FileName = fileName,
            Family = ArdFamily.Unknown,
            Source = fallbackProposal.Source,
            Template = extraction.Value.Template,
            SpecificationProposals = extraction.Value.SpecificationProposals
        };
        proposal.Template.Code ??= Path.GetFileNameWithoutExtension(fileName);

        // Decision 4, enforced here as well as in the extractor: every AI-derived field carries
        // Confidence = Low and the AiExtracted flag, unconditionally.
        foreach (var section in proposal.Template.Sections)
        foreach (var field in section.Fields)
        {
            proposal.FieldProvenance.Add(new ImportFieldProvenance
            {
                FieldKey = field.FieldKey,
                Confidence = ImportConfidence.Low,
                Reason = "AI fallback extraction (build brief 10)."
            });
        }
        foreach (var spec in proposal.SpecificationProposals)
            spec.Confidence = ImportConfidence.Low;

        foreach (var flag in extraction.Value.Flags)
            proposal.Flags.Add(flag);

        return proposal;
    }

    /// <summary>
    /// Rules that need the whole upload: an older media form is blocked by a newer twin in the
    /// batch, an EM worksheet takes suggested Alert limits from a completed COA uploaded with it,
    /// the EM area sheets share one template, proposed once (or not at all when saved), and a
    /// raw-material Specification document binds to its RM-NNN worksheet (brief 09).
    /// </summary>
    public static void ApplyBatchRules(IReadOnlyList<WorksheetImportProposal> proposals, IWorksheetImportCatalog catalog)
    {
        CultureMediaSupersession.Apply(proposals, catalog);
        EmCoaCrossCheck.Apply(proposals);
        SharedTemplates.Apply(proposals, catalog);
        RawMaterialPairing.Apply(proposals, catalog);
    }

    /// <summary>The pure core: one document stream and a catalog in, one proposal out.</summary>
    public static WorksheetImportProposal Propose(string fileName, Stream stream, IWorksheetImportCatalog catalog)
    {
        DocxDocument document;
        try
        {
            document = DocxDocumentReader.Read(stream);
        }
        catch (Exception ex) when (ex is InvalidDataException or IOException or System.Xml.XmlException
                                       or DocumentFormat.OpenXml.Packaging.OpenXmlPackageException
                                       or FormatException or InvalidOperationException or ArgumentException)
        {
            return Refused(fileName, $"The file could not be read as a Word document: {ex.Message}");
        }

        var classification = ArdFamilyClassifier.Classify(document);
        var proposal = new WorksheetImportProposal
        {
            FileName = fileName,
            Family = classification.Family,
            Source = ToSource(document)
        };

        switch (classification.Family)
        {
            case ArdFamily.CompletedCertificate:
                Flag(proposal, WorksheetImportFlagCodes.CompletedOutputNotTemplate,
                    "This is a completed Certificate of Analysis, not a worksheet; it cannot be a template source.");
                return proposal;

            case ArdFamily.Unknown:
                Flag(proposal, WorksheetImportFlagCodes.UnknownFamily, classification.Evidence switch
                {
                    "STANDARD TEST PROCEDURE" =>
                        "This is a Standard Test Procedure, not a worksheet; import it from the STP import screen.",
                    "ANALYTICAL WORKSHEET (chemical)" =>
                        "This is a finished-product chemical analytical worksheet, which cannot be imported yet. Importable: product microbiology, culture media, environmental monitoring, purified water, and raw-material chemical worksheets with their Specification documents.",
                    _ => "The document matches none of the known ARD families (product microbiology, culture media, EM, water)."
                });
                return proposal;
        }

        if (!Recognizers.TryGetValue(classification.Family, out var recognizer))
        {
            Flag(proposal, WorksheetImportFlagCodes.RecognizerPending,
                $"Recognized as {classification.Family} ('{classification.Evidence}'), whose recognizer is not built yet.");
            return proposal;
        }

        var builder = new ImportProposalBuilder(proposal, catalog);
        recognizer.Recognize(document, builder);
        if (proposal.Template is not null)
            proposal.Template.Code ??= Path.GetFileNameWithoutExtension(fileName);
        return proposal;
    }

    private static WorksheetImportProposal Refused(string fileName, string reason)
    {
        var proposal = new WorksheetImportProposal { FileName = fileName, Family = ArdFamily.Unknown };
        Flag(proposal, WorksheetImportFlagCodes.InvalidFile, reason);
        return proposal;
    }

    private static void Flag(WorksheetImportProposal proposal, string code, string message) =>
        proposal.Flags.Add(new WorksheetImportFlag { Code = code, Message = message });

    private static ImportSourceDocument ToSource(DocxDocument document) => new()
    {
        HeaderText = document.HeaderText,
        Blocks = document.Blocks.Select(block => new ImportSourceBlock
        {
            Index = block.Index,
            Kind = block.Kind.ToString(),
            Text = block.Table is null ? block.Text : null,
            Table = block.Table?.Ordinal,
            Rows = block.Table is null
                ? null
                : Enumerable.Range(0, block.Table.Rows.Count).Select(row => block.Table.RowTexts(row).ToList()).ToList()
        }).ToList()
    };
}
