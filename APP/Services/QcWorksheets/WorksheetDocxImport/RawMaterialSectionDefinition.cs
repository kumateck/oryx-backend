using System.Text.RegularExpressions;
using APP.Services.QcWorksheets.WorksheetDocxImport.TestDefinitions;
using DOMAIN.Entities.QcWorksheets;

namespace APP.Services.QcWorksheets.WorksheetDocxImport;

/// <summary>
/// Applies a <see cref="RawMaterialTestDefinition"/> to a test's buffered cells (brief 12). The
/// sheet is checked against the definition: a printed line that is one of its inputs becomes that
/// input; a required input the sheet lacks is added and flagged
/// <see cref="WorksheetImportFlagCodes.DefinitionInputAdded"/>; a line the definition does not know
/// is still kept as a field and flagged <see cref="WorksheetImportFlagCodes.DefinitionExtraLine"/>.
/// Constants are read from the sheet, never from the definition.
/// </summary>
internal sealed partial class RawMaterialSectionBody
{
    private readonly Dictionary<string, List<string>> _slots = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _literals = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _calculated = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _printedLabels = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _constants = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _variantNames = [];
    private readonly List<string> _sampleReadings = [];
    private string _standardReading;
    private StackedFractionResult _printed;
    private DefinitionVariant _variant;
    private (string Label, string Text, string Formula)? _printedVariableFormula;
    private ImportSourceLocation _end;
    private bool _emptyBody;

    /// <summary>Other tests of this worksheet the formulas refer to ("lod", "water", "loi"), still to be resolved.</summary>
    public HashSet<string> References { get; } = new(StringComparer.OrdinalIgnoreCase);

    [GeneratedRegex(@"\((?<var>[wWcCnN]\d)\)")]
    private static partial Regex VariableMarkRegex();

    [GeneratedRegex(@"\[[^\]]*\]")]
    private static partial Regex BracketRegex();

    [GeneratedRegex(@"^\s*(?:Content\s+)?Calculations?\s*:+\s*", RegexOptions.IgnoreCase)]
    private static partial Regex CalculationPrefixRegex();

    // "Measurement i: ii. Average pH:" and "Determination 1: 2: Average:" are "Measurement (i): (ii) Average pH:".
    [GeneratedRegex(@"^(?<label>[A-Za-z][A-Za-z ]*?)\s+(?:i|1)\s*[:.]\s*(?:ii|2)\s*[:.]?\s*")]
    private static partial Regex BareReplicateRegex();

    [GeneratedRegex(@"[^a-z0-9()%/+]+")]
    private static partial Regex MatchSeparatorRegex();

    [GeneratedRegex(@"\{(?<token>[^{}]+)\}")]
    private static partial Regex TokenRegex();

    private static readonly string[] Roman = ["i", "ii", "iii", "iv", "v", "vi"];

    /// <summary>Lower-case, punctuation-free text the definitions' input patterns are written against.</summary>
    private static string MatchText(string label) =>
        MatchSeparatorRegex().Replace(ImportText.StripLeaders(label ?? string.Empty).ToLowerInvariant(), " ").Trim();

    private void ApplyDefinition()
    {
        var lines = new List<(string Line, ImportSourceLocation Location)>();
        var tables = new List<(DocxTable Table, ImportSourceLocation Location)>();
        foreach (var cell in _cells)
        {
            foreach (var printed in cell.Lines)
            {
                var line = ReadInstruments(printed, cell.Location);
                if (!ImportText.IsBlank(line))
                    lines.Add((line, cell.Location));
            }

            tables.AddRange(cell.Tables.Select(table => (table, cell.Location)));
        }

        _end = lines.Count > 0 ? lines[^1].Location : _cells.LastOrDefault()?.Location ?? titleLocation;
        _emptyBody = lines.Count == 0 && tables.Count == 0;
        Section.TestDefinition = Definition.Key;

        var raw = lines.Select(item => item.Line).ToList();
        _printed = StackedFractions.Read(raw);

        // A selector-chosen variant is known before the lines are read: the line that selects it is
        // its printed formula, and the numbers it captures are read from the sheet.
        var text = sectionName + "\n" + string.Join("\n", raw);
        foreach (var variant in Definition.Variants)
        {
            var match = Regex.Match(text, variant.Selector, RegexOptions.IgnoreCase);
            if (!match.Success)
                continue;
            _variant = variant;
            _variantNames.Add(variant.Name);
            foreach (Group group in match.Groups)
            {
                if (group.Success && !int.TryParse(group.Name, out _))
                    _literals[group.Name] = group.Value;
            }

            break;
        }

        for (var index = 0; index < lines.Count; index++)
        {
            if (_printed.Consumed.Contains(index))
                continue;

            var (line, location) = lines[index];
            if (Definition.PrintsVariableFormula && TryPrintedVariableFormula(raw, ref index))
                continue;

            ReadDefinitionLine(line, location);
        }

        switch (Definition.Shape)
        {
            case DefinitionShape.Titration:
                ApplyTitration(tables);
                break;
            case DefinitionShape.Readings:
                ApplyReadings(tables);
                break;
            case DefinitionShape.Weighings:
                ApplyWeighings();
                AddOtherTables(tables, null);
                break;
            default:
                AddOtherTables(tables, null);
                AddMissingInputs();
                AddCalculations();
                break;
        }

        Section.TestDefinitionVariants = _variantNames.Distinct().ToList();
    }

    /// <summary>"Sulfated Ash = (W2-W3) x100 %" over "(W2-W1)": the formula the sheet prints over its W variables.</summary>
    private bool TryPrintedVariableFormula(IReadOnlyList<string> lines, ref int index)
    {
        if (DenominatorRegex().IsMatch(lines[index]))
            return true;

        var match = FormulaRegex().Match(lines[index]);
        if (!match.Success)
            return false;

        var numerator = match.Groups["expr"].Value.Trim();
        var text = lines[index].Trim();
        string denominator = null;
        if (index + 1 < lines.Count && DenominatorRegex().IsMatch(lines[index + 1]))
        {
            denominator = lines[index + 1].Trim().TrimEnd('-', ' ');
            text = $"{text} / {denominator}";
            index++;
        }

        // Kept as printed for now: the W inputs it uses may still be further down the cell.
        _printedVariableFormula ??= (match.Groups["label"].Value.Trim(), text, denominator is null ? numerator : $"{numerator}\n{denominator}");
        return true;
    }

    private void ReadDefinitionLine(string printed, ImportSourceLocation location)
    {
        var line = ImportText.Normalize(BracketRegex().Replace(printed, " "));
        if (ImportText.IsBlank(line) || ReadShellWeights(line, location) || RawMaterialLines.IsSkeleton(line))
            return;

        foreach (var constant in Definition.Constants.Where(constant => !_constants.Contains(constant.Key)))
        {
            var match = Regex.Match(line, constant.Pattern, RegexOptions.IgnoreCase);
            if (!match.Success)
                continue;
            AddConstant(constant, match, location);
            line = ImportText.Normalize(line.Remove(match.Index, match.Length));
        }

        var calculation = CalculationPrefixRegex().Match(line);
        if (calculation.Success)
            line = line[calculation.Length..];

        if (ImportText.IsBlank(line) || RawMaterialLines.IsSkeleton(line))
            return;

        // The printed formula of the chosen variant ("Conductivity = C1 – 0.35C2").
        if (_variant is not null && !_variant.Selector.StartsWith(@"\A") && Regex.IsMatch(line, _variant.Selector, RegexOptions.IgnoreCase))
            return;

        var labelPart = line.Contains('=') ? line[..line.IndexOf('=')] : line.TrimEnd(':', ' ');
        if (CalculationFor(labelPart) is { } own)
        {
            _printedLabels[own.Key] = labelPart.Trim().TrimEnd(':', '.', ' ');
            return;
        }

        var text = MatchText(line);
        if (text.StartsWith("attach"))
        {
            if (MatchInput(text) is { } attachment)
                AddInput(attachment, PrintedLabel(line).Label, null, location);
            else
            {
                AddEntry("attach_print_out", PrintedLabel(line).Label, WorksheetFieldType.FileUpload, null, location, "An inline attachment");
                FlagExtraLine(line, location);
            }

            return;
        }

        if (TryVariables(line, location))
            return;

        var normalized = BareReplicateRegex().Replace(line, match => $"{match.Groups["label"].Value} (i): (ii) ");
        if (RawMaterialLines.TryReplicates(normalized, out var leading, out var baseLabel, out var markers, out var average, out var tail))
        {
            foreach (var label in leading)
                ReadDefinitionBlank(new LineBlank(label, null), location);
            ReadDefinitionReplicates(baseLabel, markers, average, location);
            if (!string.IsNullOrWhiteSpace(tail))
                ReadDefinitionLine(tail, location);
            return;
        }

        if (Definition.Inputs.FirstOrDefault(input => input.KeyFromLabel) is { } open)
        {
            foreach (var part in line.Split(':').Select(part => ImportText.StripLeaders(part).Trim()).Where(part => ImportText.Canonical(part).Length > 0))
                AddInput(open, part, null, location);
            return;
        }

        var firstLabel = line.Split(':', '=')[0];
        if (line.Contains(':') && RawMaterialLines.IsCondition(firstLabel))
        {
            AddDefinitionCondition(line, location);
            return;
        }

        if (RawMaterialLines.TryBlanks(line, out var blanks, out var filledLabel, out var filledValue))
        {
            foreach (var blank in blanks.Where(blank => !RawMaterialLines.IsCaption(blank.Label)))
                ReadDefinitionBlank(blank, location);
            return;
        }

        if (filledLabel is not null)
        {
            if (_conditions.Count > 0 || RawMaterialLines.IsCondition(filledLabel))
                AddDefinitionCondition(line, location);
            else if (MatchInput(MatchText(filledLabel)) is { } filled && ImportText.Canonical(filledValue).Length <= 6)
                AddInput(filled, filledLabel, null, location);
            else
            {
                AddFilled(filledLabel, filledValue, line, location);
                FlagExtraLine(line, location);
            }

            return;
        }

        var (bareLabel, bareUnit) = PrintedLabel(line);
        if (MatchInput(MatchText(bareLabel)) is { } bare)
            AddInput(bare, bareLabel, bareUnit, location);
        else if (_conditions.Count > 0)
            AddDefinitionCondition(line, location);
        else if (!RawMaterialLines.IsCaption(line))
        {
            AddNote(line, location);
            FlagExtraLine(line, location);
        }
    }

    private void AddDefinitionCondition(string line, ImportSourceLocation location)
    {
        AddCondition(line, location);
        if (!Definition.HasConditions)
            FlagExtraLine(line, location);
    }

    private static (string Label, string Unit) PrintedLabel(string line) => BareLabel(line);

    private DefinitionCalculation CalculationFor(string label)
    {
        var text = MatchText(label);
        return text.Length == 0
            ? null
            : Definition.Calculations.FirstOrDefault(calculation => calculation.Patterns.Any(pattern => Regex.IsMatch(text, pattern)));
    }

    private DefinitionInput MatchInput(string text) =>
        text.Length == 0
            ? null
            : Definition.Inputs.FirstOrDefault(input => input.Type != WorksheetFieldType.Instrument
                                                        && input.Patterns.Any(pattern => Regex.IsMatch(text, pattern)));

    private void ReadDefinitionBlank(LineBlank blank, ImportSourceLocation location)
    {
        var label = blank.Label.Trim().TrimEnd('.', ':', ' ').TrimStart(',', ' ');
        if (ImportText.Canonical(label).Length == 0)
            return;

        if (CalculationFor(label) is { } calculation)
            _printedLabels[calculation.Key] = label;
        else if (MatchInput(MatchText(label)) is { } input)
            AddInput(input, label, blank.Unit, location);
        else
        {
            AddBlank(blank with { Label = label }, location);
            FlagExtraLine(label, location);
        }
    }

    private void ReadDefinitionReplicates(string baseLabel, List<string> markers, string average, ImportSourceLocation location)
    {
        var label = baseLabel.Trim().TrimEnd(':', '.', ' ');
        var input = MatchInput(MatchText(label));
        if (input is null)
        {
            AddReplicates(baseLabel, markers, average, location);
            FlagExtraLine(label, location);
            return;
        }

        var keys = markers.Distinct().Select(marker => AddInput(input, $"{label} {marker}", null, location)?.FieldKey).Where(key => key is not null).ToList();
        if (average is null)
            return;

        if (CalculationFor(average) is { } calculation)
            _printedLabels[calculation.Key] = average.Trim().TrimEnd(':', '=', ' ');
        else if (keys.Count > 1)
            AddCalculated($"{input.Key}_mean", average.Trim().TrimEnd(':', '=', ' '), FormulaLibrary.Average(keys.ToArray()), false, input.Unit, location,
                "Mean of the replicates (reviewer confirms)", null);
    }

    /// <summary>"Weight of empty crucible (W1) =", "Determination (C1): (C2)", "wt of empty pycnometer (w1): Temp: ˚C".</summary>
    private bool TryVariables(string line, ImportSourceLocation location)
    {
        var marks = VariableMarkRegex().Matches(line)
            .Where(mark => Definition.Inputs.Any(input => string.Equals(input.Variable, mark.Groups["var"].Value, StringComparison.OrdinalIgnoreCase)))
            .ToList();
        if (marks.Count == 0)
            return false;

        var position = 0;
        string previous = null;
        foreach (var mark in marks)
        {
            var label = ImportText.StripLeaders(line[position..mark.Index]).Trim().TrimStart(':', '=', ',', ' ').Trim();
            if (label.Length == 0)
                label = previous ?? string.Empty;
            previous = label;

            var input = Definition.Inputs.First(item => string.Equals(item.Variable, mark.Groups["var"].Value, StringComparison.OrdinalIgnoreCase));
            AddInput(input, label.Length == 0 ? null : $"{label} ({mark.Groups["var"].Value})", null, location);
            position = mark.Index + mark.Length;
        }

        var rest = ImportText.StripLeaders(line[position..]).Trim().TrimStart(':', '=', ' ').Trim();
        if (rest.Length > 0 && !Regex.IsMatch(rest, @"^(g|mg|m[lL])$") && !ImportText.IsBlank(rest))
            ReadDefinitionLine(rest, location);
        return true;
    }

    /// <summary>One field for an input: the next free slot, keyed by the definition and labelled as printed.</summary>
    private ProposedWorksheetField AddInput(DefinitionInput input, string printedLabel, string printedUnit, ImportSourceLocation location, bool added = false)
    {
        if (!_slots.TryGetValue(input.Key, out var slots))
            _slots[input.Key] = slots = [];

        var slot = slots.Count + 1;
        var label = printedLabel?.Trim().TrimEnd(':', '.', '=', ' ');
        if (string.IsNullOrWhiteSpace(label))
            label = input.Replicates > 1 ? $"{input.Label} ({Roman[Math.Min(slot, Roman.Length) - 1]})" : input.Label;

        // A misspelt "Obervation:" is still the observation a Specification binds to.
        if (input.Key == Definition.ResultKey && RawMaterialResultField.IsObservationLabel(input.Label) && !RawMaterialResultField.IsObservationLabel(label))
            label = input.Label;

        if (input.Type == WorksheetFieldType.Instrument)
        {
            AddInstrument(input.Label, null, location);
            slots.Add(Section.Fields[^1].FieldKey);
            if (added)
                FlagInputAdded(input, location);
            return Section.Fields[^1];
        }

        var key = input.KeyFromLabel ? ImportText.SnakeKey(label, 30)
            : input.Replicates > 1 || slot > 1 ? $"{input.Key}_{slot}" : input.Key;
        var numeric = input.Type is WorksheetFieldType.Number or WorksheetFieldType.Measurement;
        var field = builder.AddField(new ProposedWorksheetField
        {
            FieldKey = $"{prefix}_{key}", Label = label, Type = input.Type, Mode = WorksheetFieldMode.Entry,
            Unit = numeric ? printedUnit ?? input.Unit : null, Options = input.Options?.ToList()
        }, location, added ? ImportConfidence.Medium : ImportConfidence.High,
            added ? $"Required by the '{Definition.Title}' definition but not printed on the sheet; added"
                : input.FromEmptyCell && _emptyBody ? $"Empty observation cell: the '{Definition.Title}' definition's {input.Label.ToLowerInvariant()}"
                : $"The '{Definition.Title}' definition's input '{input.Label}'");

        slots.Add(field.FieldKey);
        if (input.Variable is not null)
            _variables[input.Variable] = field.FieldKey;
        if (added)
            FlagInputAdded(input, location);
        return field;
    }

    private void FlagInputAdded(DefinitionInput input, ImportSourceLocation location) =>
        builder.Flag(WorksheetImportFlagCodes.DefinitionInputAdded,
            $"{sectionName}: '{input.Label}' is required by the '{Definition.Title}' definition but is not printed on the sheet; it was added.", location);

    private void FlagExtraLine(string line, ImportSourceLocation location) =>
        builder.Flag(WorksheetImportFlagCodes.DefinitionExtraLine,
            $"{sectionName}: '{line}' is not part of the '{Definition.Title}' definition; it was kept as a field.", location);

    /// <summary>A constant printed on the sheet: kept as printed, and its number made available to the formulas.</summary>
    private void AddConstant(DefinitionConstant constant, System.Text.RegularExpressions.Match match, ImportSourceLocation location)
    {
        _constants.Add(constant.Key);
        var value = match.Groups["value"].Success ? match.Groups["value"].Value : null;
        var text = match.Groups["text"].Success ? ImportText.Normalize(match.Groups["text"].Value) : value;
        if (value is not null)
            _literals[constant.Key] = value;
        var unit = match.Groups["unit"].Success ? match.Groups["unit"].Value : constant.Unit;
        if (unit is not null)
            _literals[$"{constant.Key}:unit"] = unit;

        var numeric = !match.Groups["text"].Success;
        builder.AddField(new ProposedWorksheetField
        {
            FieldKey = $"{prefix}_{constant.Key}", Label = constant.Label,
            Type = numeric ? WorksheetFieldType.Number : WorksheetFieldType.ShortText,
            Mode = WorksheetFieldMode.Constant, ConstantValue = text, Unit = numeric ? constant.Unit : null
        }, location, ImportConfidence.High, value is null || numeric
            ? "Printed constant, read from the sheet"
            : $"Printed constant, read from the sheet; the formula uses its {value} {unit}".TrimEnd());
    }

    /// <summary>Adds every required input the sheet did not print. An empty observation cell is its input's blank, not an addition.</summary>
    private void AddMissingInputs()
    {
        foreach (var input in Definition.Inputs.Where(input => input.Required))
        {
            if (input.Type == WorksheetFieldType.Instrument)
            {
                var present = Section.Fields.Any(field => field.Type == WorksheetFieldType.Instrument
                                                          && input.Patterns.Any(pattern => Regex.IsMatch(field.Label.ToLowerInvariant(), pattern)));
                if (!present && !_slots.ContainsKey(input.Key))
                    AddInput(input, null, null, _end, added: true);
                continue;
            }

            EnsureSlots(input, input.Replicates);
        }
    }

    private List<string> EnsureSlots(DefinitionInput input, int count)
    {
        if (!_slots.TryGetValue(input.Key, out var slots))
            _slots[input.Key] = slots = [];
        while (slots.Count < count)
            AddInput(input, null, null, _end, added: !(input.FromEmptyCell && _emptyBody));
        return slots;
    }

    /// <summary>The scalar calculations of a <see cref="DefinitionShape.Lines"/> definition, in order.</summary>
    private void AddCalculations()
    {
        foreach (var calculation in Definition.Calculations)
        {
            var template = calculation.Formula;
            if (_variant is not null && _variant.Formulas.TryGetValue(calculation.Key, out var replaced))
            {
                if (replaced is null)
                    continue;
                template = replaced;
            }

            if (calculation.Optional && !_printedLabels.ContainsKey(calculation.Key))
                continue;

            string printedText = null;
            if (calculation.IsResult && FromPrintedTerms() is { } assembled)
                (template, printedText) = assembled;

            var formula = Expand(template, 0, null);
            var flag = WorksheetImportFlagCodes.FormulaFromDefinition;
            string label = null;
            string difference = null;

            if (calculation.IsResult && _printedVariableFormula is { } variable)
            {
                printedText = variable.Text;
                label = variable.Label.Length > 0 ? variable.Label : null;
                var parts = variable.Formula.Split('\n');
                var numerator = ToFormula(parts[0]);
                var denominator = parts.Length > 1 ? ToFormula(parts[1]) : null;
                var print = numerator is null ? null : parts.Length == 1 ? numerator : denominator is null ? null : $"({numerator}) / {denominator}";
                if (print is not null && !SameFormula(print, formula))
                {
                    difference = $"The sheet prints '{printedText}', which differs from the '{Definition.Title}' definition's formula; the printed one is used";
                    formula = print;
                    flag = WorksheetImportFlagCodes.FormulaFromPrint;
                }
            }

            AddDefinitionCalculation(calculation.Key, label ?? PrintedLabelFor(calculation), formula, calculation.Unit, calculation.IsResult,
                _end, printedText, flag, difference);
        }
    }

    private string PrintedLabelFor(DefinitionCalculation calculation) =>
        _printedLabels.TryGetValue(calculation.Key, out var printed) && printed.Count(char.IsLetter) >= 2 ? printed : calculation.Label;

    /// <summary>The first printed formula that reads, term by term, against the definition's terms.</summary>
    private (string Template, string Text)? FromPrintedTerms()
    {
        if (Definition.Terms.Count == 0)
            return null;

        foreach (var fraction in _printed.Fractions)
        {
            var template = PrintedTerms.Assemble(fraction, Definition.Terms, out var variants);
            if (template is null)
                continue;
            _variantNames.AddRange(variants);
            return (template, fraction.Text);
        }

        return null;
    }

    private ProposedWorksheetField AddDefinitionCalculation(
        string key, string label, string formula, string unit, bool isResult, ImportSourceLocation location, string printedText, string flag,
        string difference = null)
    {
        var valid = formula is not null && QcFormulaEvaluator.Analyze(formula).IsValid;
        var reason = !valid ? "The definition's formula could not be completed from this sheet; enter it before saving"
            : difference ?? Provenance(printedText);

        var field = builder.AddField(new ProposedWorksheetField
        {
            FieldKey = $"{prefix}_{key}", Label = label,
            Type = isResult ? WorksheetFieldType.Result : WorksheetFieldType.CalculatedValue,
            Mode = WorksheetFieldMode.Calculated, FormulaExpression = valid ? formula : null, Unit = unit
        }, location, valid && !PrintUnread(printedText) ? ImportConfidence.Medium : ImportConfidence.Low, reason);

        _calculated[key] = field.FieldKey;
        builder.Flag(valid ? flag : WorksheetImportFlagCodes.FormulaNeedsReview, $"{sectionName} / {label}: {reason}.", location);
        return field;
    }

    /// <summary>
    /// True when the sheet prints a formula that the definition's terms could not read. The base
    /// formula is still proposed, at Low confidence, and its provenance says so.
    /// </summary>
    private bool PrintUnread(string printedText) => printedText is null && _printed.Fractions.Count > 0 && Definition.Terms.Count > 0;

    /// <summary>Where a definition formula came from: the definition, its variants, and the formula text the sheet prints.</summary>
    private string Provenance(string printedText)
    {
        var variants = _variantNames.Distinct().ToList();
        var source = $"Formula from the '{Definition.Title}' definition" + (variants.Count == 0 ? string.Empty : $", variant: {string.Join(", ", variants)}");
        if (PrintUnread(printedText))
            return $"{source}. The sheet prints '{_printed.Fractions[0].Text}', which the definition could not read; its base formula is used (reviewer confirms)";
        return printedText is null ? $"{source}; no formula is printed on the sheet (reviewer confirms)" : $"{source}; printed: '{printedText}' (reviewer confirms)";
    }

    /// <summary>
    /// Turns a formula template into a formula: each token becomes a field key (adding the input when
    /// the sheet lacks it), a number read from the sheet, a table column of the current row, or a
    /// reference to another test that <see cref="ResolveReference"/> settles later.
    /// </summary>
    private string Expand(string template, int sample, IReadOnlyDictionary<string, string> row)
    {
        if (template is null)
            return null;

        var whole = TokenRegex().Match(template) is { Success: true } only && only.Length == template.Length;
        return TokenRegex().Replace(template, match =>
        {
            var token = match.Groups["token"].Value;
            if (token.StartsWith('$'))
                return _literals.TryGetValue(token[1..], out var literal) ? literal
                    : Reference(token[1..], sample) ?? Reference($"{token[1..]}_value", sample) ?? match.Value;

            if (token.StartsWith('@'))
            {
                References.Add(token[1..]);
                return match.Value;
            }

            if (token.StartsWith("row:"))
                return row is not null && row.TryGetValue(token[4..], out var column) ? $"{{{column}}}" : match.Value;

            if (token.StartsWith("mean:") && Definition.Inputs.FirstOrDefault(input => input.Key == token[5..]) is { } replicated)
            {
                var keys = EnsureSlots(replicated, Math.Max(replicated.Replicates, _slots.GetValueOrDefault(replicated.Key)?.Count ?? 0));
                var mean = keys.Count == 1 ? $"{{{keys[0]}}}" : FormulaLibrary.Average(keys.ToArray());
                return whole || keys.Count == 1 ? mean : $"({mean})";
            }

            if (token.StartsWith("s:"))
                return Reference(token[2..], Math.Max(sample, 1)) ?? match.Value;

            return Reference(token, 0) ?? match.Value;
        });
    }

    /// <summary>"{key}" for a calculation already added, a reading's average, or an input (its <paramref name="sample"/>-th replicate).</summary>
    private string Reference(string key, int sample)
    {
        if (sample > 0 && key == "reading" && sample <= _sampleReadings.Count)
            return $"{{{_sampleReadings[sample - 1]}}}";
        if (key == "std_reading" && _standardReading is not null)
            return $"{{{_standardReading}}}";
        if (_calculated.TryGetValue(key, out var calculated))
            return $"{{{calculated}}}";

        var slot = Math.Max(sample, 1);
        var input = Definition.Inputs.FirstOrDefault(item => item.Key == key);
        if (input is null && Regex.Match(key, @"^(?<key>.+)_(?<slot>\d)$") is { Success: true } numbered)
        {
            input = Definition.Inputs.FirstOrDefault(item => item.Key == numbered.Groups["key"].Value);
            slot = int.Parse(numbered.Groups["slot"].Value);
        }

        if (input is null)
            return null;

        // An input the sheet prints once serves every sample ("wt of sample taken" with no (i) (ii)).
        var slots = _slots.GetValueOrDefault(input.Key);
        if (slots is { Count: > 0 } && slot > slots.Count && input.Replicates == 1)
            slot = 1;
        return $"{{{EnsureSlots(input, slot)[slot - 1]}}}";
    }

    /// <summary>True when two formulas give the same number for the same inputs, whatever their arrangement.</summary>
    private static bool SameFormula(string left, string right)
    {
        var resolver = new ProbeResolver();
        return QcFormulaEvaluator.TryEvaluate(left, resolver, out var first, out _)
               && QcFormulaEvaluator.TryEvaluate(right, resolver, out var second, out _)
               && Math.Abs(first - second) <= 1e-9 * Math.Max(1, Math.Abs(first));
    }

    /// <summary>Gives every field key a distinct, stable value, so two formulas can be compared numerically.</summary>
    private sealed class ProbeResolver : IQcFormulaValueResolver
    {
        private readonly Dictionary<string, double> _values = new(StringComparer.OrdinalIgnoreCase);

        public bool TryGetScalar(string fieldKey, out double value)
        {
            if (!_values.TryGetValue(fieldKey, out value))
                _values[fieldKey] = value = 3.7 + 1.913 * _values.Count * (_values.Count + 2);
            return true;
        }

        public bool TryGetColumn(string tableFieldKey, string columnKey, out IReadOnlyList<double> values)
        {
            TryGetScalar($"{tableFieldKey}.{columnKey}", out var seed);
            values = [seed, seed + 1.3];
            return true;
        }
    }

    /// <summary>
    /// Settles a reference to another test of the worksheet: its result field when the worksheet has
    /// that test, else an Entry field added here and flagged <see cref="WorksheetImportFlagCodes.DefinitionInputAdded"/>.
    /// </summary>
    public void ResolveReference(string name, string label, string resultFieldKey)
    {
        var token = $"{{@{name}}}";
        var key = resultFieldKey;
        if (key is null)
        {
            var field = new ProposedWorksheetField
            {
                FieldKey = $"{prefix}_{name}", Label = label, Type = WorksheetFieldType.Number, Mode = WorksheetFieldMode.Entry, Unit = "%"
            };
            var position = Section.Fields.FindIndex(item => item.Type == WorksheetFieldType.Table || item.Mode == WorksheetFieldMode.Calculated);
            builder.InsertField(Section, position < 0 ? Section.Fields.Count : position, field, _end, ImportConfidence.Medium,
                $"The printed formula uses {label}, but this worksheet has no such test; added so the analyst can enter it");
            builder.Flag(WorksheetImportFlagCodes.DefinitionInputAdded,
                $"{sectionName}: the formula uses '{label}', but this worksheet has no such test; an entry was added for it.", _end);
            key = field.FieldKey;
        }

        foreach (var field in Section.Fields)
        {
            field.FormulaExpression = field.FormulaExpression?.Replace(token, $"{{{key}}}");
            field.ColumnDefinitions = field.ColumnDefinitions?.Replace(token, $"{{{key}}}");
        }
    }
}
