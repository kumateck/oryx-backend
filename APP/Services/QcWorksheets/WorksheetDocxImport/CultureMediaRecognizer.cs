using System.Globalization;
using DOMAIN.Entities.QcWorksheets;

namespace APP.Services.QcWorksheets.WorksheetDocxImport;

/// <summary>A family recognizer composes the shared primitives into one family's proposal.</summary>
public interface IArdFamilyRecognizer
{
    ArdFamily Family { get; }

    void Recognize(DocxDocument document, ImportProposalBuilder builder);
}

/// <summary>
/// Culture-medium qualification sheets ("CULTURE MEDIUM BATCH DATA"). Media come first in the
/// build order because product and water sheets reference media templates.
/// <para>
/// The medium's metadata sits in a body table of "Label: value" cells. Two form revisions
/// exist: the newer Batch No. / Issue No. / Issued By / Format No. / Medium Code form wins; the
/// older Lot No. / Date of Mfg. / Date of Expiry / Date Received form is reported as
/// <see cref="WorksheetImportFlagCodes.SupersededFormat"/>.
/// </para>
/// </summary>
public sealed class CultureMediaRecognizer : IArdFamilyRecognizer
{
    public ArdFamily Family => ArdFamily.CultureMedia;

    public void Recognize(DocxDocument document, ImportProposalBuilder builder) =>
        new CultureMediaWalker(document, builder).Run();
}

/// <summary>One recognition pass over one media document; holds the walk's state.</summary>
internal sealed partial class CultureMediaWalker(DocxDocument document, ImportProposalBuilder builder)
{
    private readonly List<(DocxBlock Block, string Text)> _references = [];
    private string _mediumName;
    private string _caption;
    private bool _inPreviousBatch;
    private bool _inReferences;

    public void Run()
    {
        var template = builder.Template;
        template.Category = WorksheetCategory.MediaQualification;
        template.Department = "Microbiology";

        var metadata = document.Blocks.FirstOrDefault(block =>
            block.Table is not null && ImportText.Canonical(block.Text).Contains("culturemediumname"));

        if (metadata is null)
            builder.Flag(WorksheetImportFlagCodes.MissingMetadata, "No 'Culture Medium Name' metadata table was found.");
        else
            ReadMetadata(metadata);

        template.Name = _mediumName is null ? "Culture Media Qualification" : $"Culture Media Qualification – {_mediumName}";
        builder.Section("Medium preparation");

        for (var index = 0; index < document.Blocks.Count; index++)
        {
            var block = document.Blocks[index];
            if (block == metadata)
                continue;

            var next = index + 1 < document.Blocks.Count ? document.Blocks[index + 1] : null;
            if (block.Table is not null)
                ReadTable(block);
            else
                ReadParagraph(block, next);
        }

        FlushReferences();
        builder.Complete();
    }

    private void StartSection(string heading)
    {
        var name = ImportText.Normalize(heading).TrimEnd(':', '.', ' ');
        if (name.Length > 1 && char.IsLetter(name[0]) && name[1] == '.')
            name = name[2..].Trim();
        if (!name.Any(char.IsLower))
            name = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(name.ToLowerInvariant());

        var canonical = ImportText.Canonical(name);
        _inPreviousBatch = canonical.StartsWith("previouslyapproved");
        _inReferences = canonical is "references" or "reference";
        builder.Section(name);
    }

    private void FlushReferences()
    {
        if (_references.Count == 0)
            return;

        builder.Section("References");
        builder.AddField(new ProposedWorksheetField
        {
            FieldKey = "references",
            Label = "References",
            Type = WorksheetFieldType.Instructions,
            Mode = WorksheetFieldMode.Constant,
            ConstantValue = string.Join("; ", _references.Select(item => item.Text))
        }, ImportProposalBuilder.At(_references[0].Block), ImportConfidence.High, "Printed reference list (SOPs, pharmacopoeia)");
    }
}
