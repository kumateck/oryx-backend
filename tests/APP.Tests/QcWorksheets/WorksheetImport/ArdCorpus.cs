using System.Text.RegularExpressions;
using APP.Services.QcWorksheets.WorksheetDocxImport;
using DOMAIN.Entities.QcWorksheets;
using Xunit;

namespace APP.Tests.QcWorksheets.WorksheetImport;

/// <summary>
/// Runs only when <c>QC_ARD_CORPUS_DIR</c> points at the lab's 49 real ARD files. The files
/// carry staff names and are not committed, so CI (variable unset) skips these.
/// </summary>
public sealed class CorpusFactAttribute : FactAttribute
{
    public const string Variable = "QC_ARD_CORPUS_DIR";

    public CorpusFactAttribute()
    {
        var directory = Environment.GetEnvironmentVariable(Variable);
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            Skip = $"{Variable} is not set to the ARD corpus directory.";
    }
}

internal sealed record CorpusFile(string RelativePath, ArdFamily Expected, WorksheetImportProposal Proposal, DocxDocument Document);

/// <summary>
/// Loads the real corpus from <see cref="CorpusFactAttribute.Variable"/> (never copied into the
/// repository) as one upload batch, so the batch-level supersession rule applies.
/// </summary>
internal static class ArdCorpus
{
    public static List<CorpusFile> Load()
    {
        var root = Environment.GetEnvironmentVariable(CorpusFactAttribute.Variable)!;
        var files = Directory.EnumerateFiles(root, "*.docx", SearchOption.AllDirectories)
            .Where(path => !Path.GetFileName(path).StartsWith("~$", StringComparison.Ordinal))
            .OrderBy(path => path, StringComparer.Ordinal)
            .Select(path =>
            {
                var relative = Path.GetRelativePath(root, path);
                using var stream = File.OpenRead(path);
                var proposal = WorksheetDocxImportService.Propose(Path.GetFileName(path), stream, InMemoryWorksheetImportCatalog.Empty);
                using var again = File.OpenRead(path);
                return new CorpusFile(relative, ExpectedFamily(relative), proposal, DocxDocumentReader.Read(again));
            })
            .ToList();
        WorksheetDocxImportService.ApplyBatchRules(files.Select(file => file.Proposal).ToList(), InMemoryWorksheetImportCatalog.Empty);
        return files;
    }

    /// <summary>The corpus is filed by family; product sheets are the "FP" / "Finished Product" files.</summary>
    public static ArdFamily ExpectedFamily(string relative)
    {
        var upper = relative.ToUpperInvariant();
        if (upper.Contains("COA")) return ArdFamily.CompletedCertificate;
        if (upper.Contains("MONITORING WORKSHEET")) return ArdFamily.EnvironmentalMonitoring;
        if (upper.Contains("WATER")) return ArdFamily.PurifiedWater;
        if (Regex.IsMatch(upper, @"\bFP\b|FINISHED PRODUCT")) return ArdFamily.ProductMicro;
        return ArdFamily.CultureMedia;
    }
}
