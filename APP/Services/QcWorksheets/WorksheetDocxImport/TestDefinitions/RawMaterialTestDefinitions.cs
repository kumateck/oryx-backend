using System.Text.RegularExpressions;

namespace APP.Services.QcWorksheets.WorksheetDocxImport.TestDefinitions;

/// <summary>
/// The definitions, and the rule that picks one (brief 12, locked decision 1): by the test's name
/// through each definition's synonym patterns, never by its position on the sheet. An "Assay" is
/// further told apart by what it prints — a titration grid, peak areas or absorbances — because the
/// sheets name the technique loosely ("Assay", or "Assay – UV" over an HPLC layout).
/// </summary>
public static partial class RawMaterialTestDefinitions
{
    /// <summary>In matching order: a physical test named under "Identity Test B – …" wins over the identity catch-all.</summary>
    public static readonly IReadOnlyList<RawMaterialTestDefinition> All =
    [
        .. AssayDefinitions.All,
        .. GravimetricDefinitions.All,
        .. PhysicalConstantDefinitions.All,
        .. CapsuleShellDefinitions.All,
        .. ObservationDefinitions.All
    ];

    [GeneratedRegex(@"^(identity|identification)( tests?)?\b ?")]
    private static partial Regex IdentityPrefixRegex();

    [GeneratedRegex(@"^(test )?[a-f]( |$)")]
    private static partial Regex IdentityLetterRegex();

    [GeneratedRegex(@"[^a-z0-9]+")]
    private static partial Regex SeparatorRegex();

    /// <summary>Lower-case words only: "Identity Test A – Melting Point:" → "identity test a melting point".</summary>
    public static string NormalizeName(string name) =>
        SeparatorRegex().Replace((name ?? string.Empty).ToLowerInvariant().Replace("&amp;", " "), " ").Trim();

    public static RawMaterialTestDefinition Find(string key) => All.FirstOrDefault(definition => definition.Key == key);

    /// <summary>The definition a test name selects on its own, or null.</summary>
    public static RawMaterialTestDefinition ByName(string sectionName)
    {
        var name = NormalizeName(sectionName);
        if (name.Length == 0)
            return null;

        var identity = IdentityPrefixRegex().Match(name);
        if (!identity.Success)
            return All.Where(definition => definition.Key != "identity_reaction").FirstOrDefault(definition => Matches(definition, name));

        // "Identity Test B – Refractive Index" is a refractive index; "Identity Test: Reaction of
        // Chlorides" is an identity reaction, not the chlorides limit test.
        var own = name[identity.Length..];
        var letter = IdentityLetterRegex().Match(own);
        var technique = letter.Success ? own[letter.Length..].Trim() : own;
        return All.Where(definition => definition.IdentityTest && definition.Key != "identity_reaction")
                   .FirstOrDefault(definition => Matches(definition, technique))
               ?? All.Where(definition => definition.Key == "identity_reaction")
                   .FirstOrDefault(definition => Matches(definition, technique) || Matches(definition, own));
    }

    /// <summary>
    /// The definition for a section: its name, and for an assay the layout it prints.
    /// </summary>
    public static RawMaterialTestDefinition Resolve(string sectionName, IReadOnlyList<string> lines, IReadOnlyList<DocxTable> tables)
    {
        var name = NormalizeName(sectionName);
        if (!name.StartsWith("assay"))
            return ByName(sectionName);

        if (tables.Any(TitrationGrid.IsTitration))
            return AssayDefinitions.Titration;

        var named = AssayDefinitions.All.FirstOrDefault(definition => Matches(definition, name));
        var text = string.Join(" ", lines).ToLowerInvariant();
        var readings = tables.Select(ReadingsGrid.Read).FirstOrDefault(grid => grid is not null);
        if (readings is not null)
        {
            // Readings that are not injections are absorbances when the name or the printed formula says so.
            var absorbances = readings.IsAbsorbance
                              || (!readings.IsInjections && !text.Contains("peak") && (named == AssayDefinitions.Uv || text.Contains(" abs")));
            return absorbances ? AssayDefinitions.Uv : AssayDefinitions.Hplc;
        }

        if (named is not null)
            return named;

        // No table and no technique in the name: the printed formula says which assay it is.
        if (text.Contains("titre"))
            return AssayDefinitions.Titration;
        if (text.Contains("peak"))
            return AssayDefinitions.Hplc;
        return text.Contains("spl abs") || text.Contains("absorbance") ? AssayDefinitions.Uv : null;
    }

    private static bool Matches(RawMaterialTestDefinition definition, string name) =>
        definition.NamePatterns.Any(pattern => Regex.IsMatch(name, pattern));
}
