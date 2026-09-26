using System.Text.RegularExpressions;
using DOMAIN.Entities.QcWorksheets;

namespace APP.Services.QcWorksheets.WorksheetDocxImport;

public sealed record ArdClassification(ArdFamily Family, string Evidence);

/// <summary>
/// Assigns the document family from the markers found in the corpus (brief 07). Order
/// matters:
/// <list type="number">
/// <item>certificates first — a COA reuses the EM vocabulary but is a completed output;</item>
/// <item>culture media before water — every media sheet says "distilled water";</item>
/// <item>EM before product — both are "RAW DATA" sheets;</item>
/// <item>the raw-material worksheet and Specification document (brief 09) before the chemical
/// "ANALYTICAL WORKSHEET" fallback, which stays Unknown for finished-product chemical sheets.</item>
/// </list>
/// Marker comparison ignores case, spacing and punctuation: the EM header really reads
/// "ENVIRONMENTAL MONITORING  RAW DATA" and COA headers run their words together.
/// </summary>
public static partial class ArdFamilyClassifier
{
    // "SPC No.: QCD/SPC/RM/012" or, on the capsule shell, "Specification No.: QCD/SPC/RM/193".
    // The worksheet's own "Spec. No.: NQC/RM/SPC/012" does not match: "Spec." is neither form.
    [GeneratedRegex(@"\b(?:SPC|Specification)\s*No\.?\s*:\s*\S*/RM/", RegexOptions.IgnoreCase)]
    private static partial Regex RawMaterialSpecificationNumberRegex();

    public static ArdClassification Classify(DocxDocument document)
    {
        var header = ImportText.Canonical(document.HeaderText);
        var firstTable = document.Tables.FirstOrDefault();
        var firstTableText = firstTable is null
            ? string.Empty
            : ImportText.Canonical(string.Join(" ", firstTable.Rows.SelectMany((_, row) => firstTable.RowTexts(row))));
        var body = ImportText.Canonical(string.Join(" ", document.Blocks.Take(40).Select(block => block.Text)));
        var all = header + " " + body;

        if (all.Contains("certificateofanalysis"))
            return new ArdClassification(ArdFamily.CompletedCertificate, "CERTIFICATE OF ANALYSIS");

        if (header.Contains("rawmaterialanalyticalworksheet"))
            return new ArdClassification(ArdFamily.RawMaterialChemical, "RAW MATERIAL ANALYTICAL WORKSHEET");

        if (header.Contains("specification") && RawMaterialSpecificationNumberRegex().IsMatch(document.HeaderText))
            return new ArdClassification(ArdFamily.RawMaterialSpecification, "SPECIFICATION + SPC No. …/RM/…");

        if (all.Contains("culturemediumname"))
            return new ArdClassification(ArdFamily.CultureMedia, "Culture Medium Name");

        if (all.Contains("environmentalmonitoringrawdata"))
            return new ArdClassification(ArdFamily.EnvironmentalMonitoring, "ENVIRONMENTAL MONITORING RAW DATA");

        if (all.Contains("analyticalrawdatamicrobiology"))
            return new ArdClassification(ArdFamily.ProductMicro, "ANALYTICAL RAW DATA – MICROBIOLOGY");

        // "WATER" must be the sheet's own subject — in the running header or the metadata
        // table — not any mention of water in the method text. The header must also be the
        // MICROBIOLOGY worksheet: a raw-material chemical sheet is also headed "ANALYTICAL
        // WORKSHEET" and its Solubility row reads "Water:", which once misfiled it as water.
        if (header.Contains("microbiologyanalyticalworksheet")
            && (header.Contains("water") || firstTableText.Contains("water")))
            return new ArdClassification(ArdFamily.PurifiedWater, "MICROBIOLOGY ANALYTICAL WORKSHEET + WATER");

        // Known documents the importer does not build templates from; the evidence names
        // them so the reviewer is told what the file is rather than just "unknown".
        if (header.Contains("standardtestprocedure"))
            return new ArdClassification(ArdFamily.Unknown, "STANDARD TEST PROCEDURE");

        if (header.Contains("analyticalworksheet"))
            return new ArdClassification(ArdFamily.Unknown, "ANALYTICAL WORKSHEET (chemical)");

        return new ArdClassification(ArdFamily.Unknown, null);
    }
}
