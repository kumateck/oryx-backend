using DOMAIN.Entities.QcWorksheets;
using Microsoft.AspNetCore.Http;

namespace APP.Services.QcWorksheets.WorksheetDocxImport;

public interface IWorksheetDocxImportService
{
    /// <summary>One proposal per file. Writes nothing.</summary>
    Task<List<WorksheetImportProposal>> ProposeAsync(IEnumerable<IFormFile> files, CancellationToken cancellationToken = default);
}

/// <summary>
/// Turns ARD worksheets into reviewable proposals (brief 07, Phase B): read → classify →
/// family recognizer. Certificates and unknown files stop at classification with a flag;
/// families whose recognizer is not built yet stop with <see cref="WorksheetImportFlagCodes.RecognizerPending"/>.
/// Nothing is persisted and nothing is approved automatically.
/// </summary>
public class WorksheetDocxImportService(IWorksheetImportCatalogLoader catalogLoader) : IWorksheetDocxImportService
{
    private static readonly IReadOnlyDictionary<ArdFamily, IArdFamilyRecognizer> Recognizers =
        new IArdFamilyRecognizer[] { new CultureMediaRecognizer(), new ProductMicroRecognizer() }.ToDictionary(recognizer => recognizer.Family);

    public async Task<List<WorksheetImportProposal>> ProposeAsync(
        IEnumerable<IFormFile> files, CancellationToken cancellationToken = default)
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
            proposals.Add(Propose(file.FileName, stream, catalog));
        }

        // Needs the whole upload: an older media form is blocked by a newer twin in the batch.
        CultureMediaSupersession.Apply(proposals, catalog);
        return proposals;
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
                Flag(proposal, WorksheetImportFlagCodes.UnknownFamily,
                    "The document matches none of the known ARD families (product, culture media, EM, water).");
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
