using DOMAIN.Entities.QcWorksheets;

namespace APP.Services.QcWorksheets.WorksheetDocxImport.TestDefinitions;

/// <summary>How a definition's section is laid out on the sheet, which decides how its table and formula are built.</summary>
public enum DefinitionShape
{
    /// <summary>"Label: ____" lines only; calculations are scalar fields.</summary>
    Lines,

    /// <summary>Blank / Sample 1 / Sample 2 burette readings; the assay is calculated once per sample row.</summary>
    Titration,

    /// <summary>Replicate readings of a standard and of each sample (peak areas, absorbances); one assay per sample.</summary>
    Readings,

    /// <summary>Numbered individual weighings ("01) … 20)") with their total and average.</summary>
    Weighings
}

/// <summary>
/// One input a test requires (brief 12). It is found on the sheet by <see cref="Patterns"/> (regular
/// expressions over the printed label) or by its printed <see cref="Variable"/> ("W1"); a required
/// input the sheet lacks is added and flagged <see cref="WorksheetImportFlagCodes.DefinitionInputAdded"/>.
/// </summary>
public sealed record DefinitionInput(string Key, string Label, WorksheetFieldType Type = WorksheetFieldType.Number, string Unit = null)
{
    /// <summary>1, or 2 for "(i) (ii)" readings: the fields are <c>key_1</c>, <c>key_2</c>.</summary>
    public int Replicates { get; init; } = 1;

    /// <summary>Added when the sheet lacks it. An optional input is still added when the chosen formula uses it.</summary>
    public bool Required { get; init; } = true;

    public IReadOnlyList<string> Patterns { get; init; } = [];

    /// <summary>The printed formula variable that names this input: "w1", "c2".</summary>
    public string Variable { get; init; }

    public IReadOnlyList<string> Options { get; init; }

    /// <summary>The empty observation cell under the test name is this input's blank.</summary>
    public bool FromEmptyCell { get; init; }

    /// <summary>Every printed label is one of these, keyed by the label itself (the Solubility solvents).</summary>
    public bool KeyFromLabel { get; init; }
}

public sealed record DefinitionColumn(string Key, string Label, WorksheetFieldType Type = WorksheetFieldType.Number, string Unit = null, string Formula = null);

/// <summary>The table a test fills: fixed row labels and columns, some calculated per row.</summary>
public sealed record DefinitionTable(string Key, string Label, string RowHeader, IReadOnlyList<string> Rows, IReadOnlyList<DefinitionColumn> Columns);

/// <summary>
/// A constant printed on the sheet (locked decision 5: never taken from the definition).
/// <see cref="Pattern"/> finds it; its <c>text</c> group is kept as printed and its <c>value</c>
/// group, when present, is the number a formula uses.
/// </summary>
public sealed record DefinitionConstant(string Key, string Label, string Pattern, string Unit = null);

/// <summary>
/// One calculated field. <see cref="Formula"/> is a template over input keys: <c>{w1}</c> an input,
/// <c>{reading_2}</c> a replicate, <c>{mean:reading}</c> the mean of an input's replicates,
/// <c>{$equiv}</c> a number read from the sheet, <c>{@lod}</c> another test's result on the same
/// worksheet, <c>{s:weight}</c> the current sample's replicate and <c>{row:titre}</c> a column of the
/// current table row.
/// </summary>
public sealed record DefinitionCalculation(string Key, string Label, string Formula, string Unit = null, bool IsResult = false)
{
    /// <summary>Printed lines that are this calculation's own blank ("Average %Assay =").</summary>
    public IReadOnlyList<string> Patterns { get; init; } = [];

    /// <summary>Emitted only when the sheet prints its label ("wt of water (w2 – w1):").</summary>
    public bool Optional { get; init; }
}

/// <summary>A named alternative, chosen when <see cref="Selector"/> matches the section's name and printed lines.</summary>
public sealed record DefinitionVariant(string Name, string Selector)
{
    /// <summary>Calculation key → the formula template this variant uses instead.</summary>
    public IReadOnlyDictionary<string, string> Formulas { get; init; } = new Dictionary<string, string>();
}

/// <summary>
/// One term of a printed formula ("Factor", "(100 – LOD)") and the template fragment it stands for.
/// A formula printed as a stacked fraction is read term by term, so the variant is exactly what the
/// sheet prints; <see cref="Variant"/> names the alternative a term selects.
/// </summary>
public sealed record DefinitionTerm(string Pattern, string Symbol, string Variant = null);

/// <summary>
/// What one raw-material test is (brief 12): the inputs it requires, its table, the constants to
/// read from the sheet, its calculations and the field a Specification binds to. Code, not data.
/// </summary>
public sealed record RawMaterialTestDefinition
{
    public string Key { get; init; }
    public string Title { get; init; }
    public DefinitionShape Shape { get; init; } = DefinitionShape.Lines;

    /// <summary>Synonyms: regular expressions over the lower-case, punctuation-free test name.</summary>
    public IReadOnlyList<string> NamePatterns { get; init; } = [];

    /// <summary>True when the name patterns apply to an "Identity Test X – …" sub-test's own name too.</summary>
    public bool IdentityTest { get; init; }

    public IReadOnlyList<DefinitionInput> Inputs { get; init; } = [];
    public DefinitionTable Table { get; init; }
    public IReadOnlyList<DefinitionConstant> Constants { get; init; } = [];
    public IReadOnlyList<DefinitionCalculation> Calculations { get; init; } = [];
    public IReadOnlyList<DefinitionVariant> Variants { get; init; } = [];
    public IReadOnlyList<DefinitionTerm> Terms { get; init; } = [];

    /// <summary>The field a Specification characteristic binds to: a calculation's or an input's key.</summary>
    public string ResultKey { get; init; }

    /// <summary>Printed method conditions (HPLC) belong to the test and are kept as one Constant.</summary>
    public bool HasConditions { get; init; }

    /// <summary>
    /// The sheet prints the formula over its W variables: it is compared with the definition's, and
    /// where they differ the printed one wins and is flagged.
    /// </summary>
    public bool PrintsVariableFormula { get; init; }
}
