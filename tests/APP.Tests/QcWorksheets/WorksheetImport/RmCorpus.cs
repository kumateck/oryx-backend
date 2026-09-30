using APP.Services.QcWorksheets.WorksheetDocxImport;
using DOMAIN.Entities.QcWorksheets;
using Xunit;

namespace APP.Tests.QcWorksheets.WorksheetImport;

/// <summary>
/// Runs only when <c>QC_RM_CORPUS_DIR</c> points at the 7 raw-material worksheet / Specification
/// pairs (brief 09). The files carry staff names and are never committed, so CI skips these;
/// <see cref="RawMaterialImportTests"/> covers the same rules with synthetic documents.
/// </summary>
public sealed class RmCorpusFactAttribute : FactAttribute
{
    public const string Variable = "QC_RM_CORPUS_DIR";

    public RmCorpusFactAttribute()
    {
        var directory = Environment.GetEnvironmentVariable(Variable);
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            Skip = $"{Variable} is not set to the raw-material corpus directory.";
    }
}

internal sealed record RmFile(string FileName, string Key, bool IsSpecification, WorksheetImportProposal Proposal, DocxDocument Document);

/// <summary>Loads the 14 files as one upload, so the pairing batch rule applies.</summary>
internal static class RmCorpus
{
    public static readonly string[] Keys = ["012", "017", "021", "024", "167", "179", "193"];

    public static List<RmFile> Load(IWorksheetImportCatalog? catalog = null, Func<string, bool>? include = null)
    {
        catalog ??= InMemoryWorksheetImportCatalog.Empty;
        var root = Environment.GetEnvironmentVariable(RmCorpusFactAttribute.Variable)!;
        var files = Directory.EnumerateFiles(root, "*.docx")
            .Where(path => !Path.GetFileName(path).StartsWith("~$", StringComparison.Ordinal))
            .Where(path => include?.Invoke(Path.GetFileName(path)) ?? true)
            .OrderBy(path => path, StringComparer.Ordinal)
            .Select(path =>
            {
                var name = Path.GetFileName(path);
                using var stream = File.OpenRead(path);
                var proposal = WorksheetDocxImportService.Propose(name, stream, catalog);
                using var again = File.OpenRead(path);
                return new RmFile(name, name[..3], name.Contains("spec", StringComparison.OrdinalIgnoreCase), proposal,
                    DocxDocumentReader.Read(again));
            })
            .ToList();
        WorksheetDocxImportService.ApplyBatchRules(files.Select(file => file.Proposal).ToList(), catalog);
        return files;
    }

    public static List<ProposedWorksheetField> Fields(WorksheetImportProposal proposal) =>
        proposal.Template?.Sections.SelectMany(section => section.Fields).ToList() ?? [];
}
