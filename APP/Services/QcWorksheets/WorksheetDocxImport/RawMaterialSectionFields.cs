using System.Text.RegularExpressions;
using DOMAIN.Entities.QcWorksheets;

namespace APP.Services.QcWorksheets.WorksheetDocxImport;

/// <summary>Blank, replicate and printed-formula fields of <see cref="RawMaterialSectionBody"/>.</summary>
internal sealed partial class RawMaterialSectionBody
{
    private static readonly string[] LongTextLabels =
        ["preparation", "preparations", "observation", "observations", "inference", "testsolution", "referencesolution"];

    // "Weight of empty crucible (W1) =", "wt of pycnometer + water (w2):", "wt of sample (w1)".
    [GeneratedRegex(@"^(?<label>.*?\S)\s*\((?<var>[wW]\d)\)\s*[:=]?\s*(?<rest>.*)$")]
    private static partial Regex VariableRegex();

    // "wt of water (w2 – w1):" — a printed difference of two weights.
    [GeneratedRegex(@"^(?<label>[^=:()]*?\S)\s*\((?<expr>[wW]\d\s*[–—-]\s*[wW]\d)\)\s*:?\s*$")]
    private static partial Regex DerivedRegex();

    // "Sulfated Ash = (W2-W3) x100 %", "= (w3 – w1) = ____-____ =", "Result = W1 – W2 =".
    [GeneratedRegex(@"^(?<label>[^=]*?)\s*=\s*(?<expr>[\s()wW\d–—\-xX×*+/%.]*[wW]\d[\s()wW\d–—\-xX×*+/%.]*?)\s*(?:=.*)?$")]
    private static partial Regex FormulaRegex();

    // The denominator printed under the fraction bar: "(W2-W1)", "(w2 – w1) -".
    [GeneratedRegex(@"^\(\s*[wW]\d\s*[–—-]\s*[wW]\d\s*\)\s*-?\s*$")]
    private static partial Regex DenominatorRegex();

    [GeneratedRegex(@"[wW](\d)")]
    private static partial Regex VariableReferenceRegex();

    // "01) 02) 03) 04)": the individual capsule-shell weights.
    [GeneratedRegex(@"^(?:\d{2}\)\s*)+$")]
    private static partial Regex ShellWeightRegex();

    private void AddBlank(LineBlank blank, ImportSourceLocation location)
    {
        var label = blank.Label.Trim().TrimEnd('.', ':');
        var canonical = ImportText.Canonical(label);
        if (canonical.Length == 0)
            return;

        if (RawMaterialLines.WavelengthRegex().IsMatch(label))
        {
            AddEntry($"absorbance_{canonical}", $"Absorbance at {label}", WorksheetFieldType.Number, null, location, "Absorbance blank at a printed wavelength");
            return;
        }

        if (_shellWeightsTable is not null && Regex.IsMatch(canonical, @"^(average)?weightof(\d+)?shells?$"))
        {
            var sum = canonical.StartsWith("average") ? "AVG" : "SUM";
            AddCalculated(ImportText.SnakeKey(label, 30), label, $"{sum}({{{_shellWeightsTable}.weight}})", sum == "AVG",
                "mg", location, $"{sum} of the individual shell weights", null);
            return;
        }

        var (type, unit, reason) = _solubility
            ? (WorksheetFieldType.ShortText, (string)null, "Solubility in one solvent, recorded as observed")
            : LongTextLabels.Contains(canonical)
                ? (WorksheetFieldType.LongText, null, $"'{label}:' blank: free-text observation")
                : Numeric(canonical, blank.Unit);

        var confidence = type == WorksheetFieldType.ShortText && !_solubility ? ImportConfidence.Medium : ImportConfidence.High;
        AddEntry(ImportText.SnakeKey(label, 30), label, type, unit, location, reason ?? $"'{label}:' blank", confidence);
    }

    private static (WorksheetFieldType, string, string) Numeric(string canonical, string printedUnit)
    {
        if (canonical.StartsWith("weight") || canonical.StartsWith("wt"))
            return (WorksheetFieldType.Number, printedUnit ?? "g", "Weight blank");
        if (canonical.StartsWith("vol"))
            return (WorksheetFieldType.Number, printedUnit ?? "mL", "Volume blank");
        if (canonical.StartsWith("temp"))
            return (WorksheetFieldType.Number, printedUnit ?? "°C", "Temperature blank");
        if (canonical.StartsWith("factor") || canonical.StartsWith("angle") || canonical.StartsWith("determination")
            || canonical.StartsWith("measurement") || printedUnit is not null)
            return (WorksheetFieldType.Number, printedUnit, "Numeric reading blank");
        return (WorksheetFieldType.ShortText, null, null);
    }

    /// <summary>"Determination (i): (ii) mean:" → two Number entries and their calculated mean.</summary>
    private void AddReplicates(string baseLabel, List<string> markers, string average, ImportSourceLocation location)
    {
        var label = baseLabel.Trim().TrimEnd(':', '.');
        var (_, unit, _) = Numeric(ImportText.Canonical(label), null);
        var keys = markers.Distinct().Select(marker => AddEntry(
            $"{ImportText.SnakeKey(label, 24)}_{marker.Trim('(', ')')}", $"{label} {marker}", WorksheetFieldType.Number, unit, location,
            "Replicate determination").FieldKey).ToList();

        if (average is null || keys.Count < 2)
            return;

        var averageLabel = average.Trim().TrimEnd(':');
        AddCalculated(ImportText.SnakeKey(label, 24) + "_mean", averageLabel, FormulaLibrary.Average(keys.ToArray()), true,
            averageLabel.Contains('%') ? "%" : unit, location, "Mean of the replicate determinations (reviewer confirms)", null);
    }

    private bool TryVariableLine(string line, ImportSourceLocation location)
    {
        var match = VariableRegex().Match(line);
        if (!match.Success)
            return false;

        var variable = match.Groups["var"].Value.ToLowerInvariant();
        var label = $"{match.Groups["label"].Value.Trim()} ({match.Groups["var"].Value})";
        var field = AddEntry(variable, label, WorksheetFieldType.Number, "g", location, "Printed weighing variable of the formula below it");
        _variables[variable] = field.FieldKey;

        var rest = match.Groups["rest"].Value.Trim();
        if (rest.Length > 0 && !ImportText.IsBlank(rest) && RawMaterialLines.TryBlanks(rest, out var blanks, out _, out _))
            foreach (var blank in blanks)
                AddBlank(blank, location);
        return true;
    }

    private bool TryDerivedLine(string line, ImportSourceLocation location)
    {
        var match = DerivedRegex().Match(line);
        var formula = match.Success ? ToFormula(match.Groups["expr"].Value) : null;
        if (formula is null)
            return false;

        var label = $"{match.Groups["label"].Value.Trim()} ({match.Groups["expr"].Value.Trim()})";
        AddCalculated(ImportText.SnakeKey(match.Groups["label"].Value, 30), label, formula, false, "g", location,
            "Difference printed beside the label", WorksheetImportFlagCodes.FormulaFromPrint);
        return true;
    }

    /// <summary>A printed formula over the W variables, with its denominator on the next line.</summary>
    private bool TryFormulaLine(IReadOnlyList<string> lines, ref int index, ImportSourceLocation location)
    {
        var match = FormulaRegex().Match(lines[index]);
        if (!match.Success || _variables.Count == 0)
            return false;

        var expression = match.Groups["expr"].Value.Trim();
        var numerator = ToFormula(expression);
        if (numerator is null)
            return false;

        var printed = lines[index].Trim();
        var formula = numerator;
        if (index + 1 < lines.Count && DenominatorRegex().IsMatch(lines[index + 1])
            && ToFormula(lines[index + 1].Trim().TrimEnd('-', ' ')) is { } denominator)
        {
            formula = $"({numerator}) / {denominator}";
            index++;
        }

        var label = match.Groups["label"].Value.Trim();
        var percent = expression.Contains('%') || expression.Contains("100");
        AddCalculated("result", label.Length == 0 ? sectionName : label, formula, true, percent ? "%" : null, location,
            $"Formula as printed: '{printed}' (reviewer confirms)", WorksheetImportFlagCodes.FormulaFromPrint);
        return true;
    }

    /// <summary>"(W2-W3) x100 %" → "({…_w2} - {…_w3}) * 100"; null when a variable is not on the sheet.</summary>
    private string ToFormula(string printed)
    {
        // Operators first, while the text holds only W variables: a field key may contain an "x".
        var text = Regex.Replace(printed.Replace('–', '-').Replace('—', '-').Replace("%", string.Empty), @"\s*[xX×]\s*", " * ");
        text = ImportText.Normalize(Regex.Replace(text, @"\s*-\s*", " - ")).Trim();

        var missing = false;
        text = VariableReferenceRegex().Replace(text, match =>
        {
            if (_variables.TryGetValue("w" + match.Groups[1].Value, out var key))
                return $"{{{key}}}";
            missing = true;
            return match.Value;
        });
        return missing || !QcFormulaEvaluator.Analyze(text).IsValid ? null : text;
    }

    /// <summary>"Calculation:" left blank, or an HPLC / titration assay: the formula is left for the reviewer.</summary>
    private void AddUnprintedCalculation(ImportSourceLocation location, string printed)
    {
        if (_calculationAdded)
            return;
        _calculationAdded = true;

        var assay = ImportText.Canonical(sectionName).Contains("assay") || ImportText.Canonical(printed).Contains("content");
        AddCalculated("result", assay ? "% Assay" : sectionName, null, true, assay ? "%" : null, location,
            "The calculation is not printed as a formula; enter it before saving", WorksheetImportFlagCodes.FormulaNeedsReview);
    }

    private ProposedWorksheetField AddCalculated(
        string key, string label, string formula, bool isResult, string unit, ImportSourceLocation location, string reason, string flag)
    {
        var field = builder.AddField(new ProposedWorksheetField
        {
            FieldKey = $"{prefix}_{key}", Label = label,
            Type = isResult ? WorksheetFieldType.Result : WorksheetFieldType.CalculatedValue,
            Mode = WorksheetFieldMode.Calculated, FormulaExpression = formula, Unit = unit
        }, location, flag == WorksheetImportFlagCodes.FormulaNeedsReview ? ImportConfidence.Low : ImportConfidence.Medium, reason);

        if (flag is not null)
            builder.Flag(flag, $"{sectionName} / {label}: {reason}.", location);
        return field;
    }

    /// <summary>
    /// A printed line with no blank of its own: a bare label ("Observation", "Weight of sample") is
    /// that label's blank; anything else is kept as a printed note, so nothing on the sheet is dropped.
    /// </summary>
    private void AddBareLine(string line, ImportSourceLocation location)
    {
        var (label, unit) = BareLabel(line);
        var canonical = ImportText.Canonical(label);
        if (LongTextLabels.Contains(canonical) || canonical.StartsWith("weight") || canonical.StartsWith("wt") || canonical.StartsWith("vol"))
        {
            AddBlank(new LineBlank(label, unit), location);
            return;
        }

        AddNote(line, location);
    }

    private void AddNote(string line, ImportSourceLocation location) =>
        builder.AddField(new ProposedWorksheetField
        {
            FieldKey = $"{prefix}_{ImportText.SnakeKey(line, 30)}_note", Label = line.Length > 80 ? line[..80].TrimEnd() + "…" : line,
            Type = WorksheetFieldType.Instructions, Mode = WorksheetFieldMode.Constant, ConstantValue = line
        }, location, ImportConfidence.Medium, "Printed line with no blank of its own, kept as a note");

    /// <summary>"Volume of solution S taken ____ ml" → ("Volume of solution S taken", "mL").</summary>
    private static (string Label, string Unit) BareLabel(string line)
    {
        var text = ImportText.StripLeaders(line).Trim().TrimEnd(':', '=', '.', ' ');
        var unit = Regex.Match(text, @"\s(g|mg|m[lL]|%|[˚°]\s*C)$");
        return unit.Success
            ? (text[..unit.Index].TrimEnd(':', '=', ' '), unit.Groups[1].Value.Replace("ml", "mL").Replace("˚", "°").Replace(" ", string.Empty))
            : (text, null);
    }

    private bool ReadShellWeights(string line, ImportSourceLocation location)
    {
        if (!ShellWeightRegex().IsMatch(line))
            return false;
        _shellWeightsLocation ??= location;
        _shellWeights += Regex.Matches(line, @"\d{2}\)").Count;
        return true;
    }

    /// <summary>"01) … 20)" → a Table with one fixed row per shell and an entered weight column.</summary>
    private void FlushShellWeights()
    {
        if (_shellWeights == 0)
            return;

        var rows = Enumerable.Range(1, _shellWeights).Select(number => number.ToString("00")).ToList();
        _shellWeightsTable = builder.AddTable($"{prefix}_individual_weights", "Individual weights",
        [
            new GridColumn { Key = "shell", Label = "Shell", Type = WorksheetFieldType.ShortText, FixedValues = rows, RowHeader = true, Reason = "Printed numbering" },
            new GridColumn { Key = "weight", Label = "Weight", Type = WorksheetFieldType.Number, Unit = "mg", Confidence = ImportConfidence.Medium,
                Reason = "Unit not printed; mg as in the Specification (reviewer confirms)" }
        ], _shellWeightsLocation, "Numbered weighing blanks").FieldKey;
        _shellWeights = 0;
    }
}
