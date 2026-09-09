using System.Text.Json;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Formulas;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

#nullable enable

namespace APP.Services.Formulas;

internal static class FormulaResponseInputResolver
{
    private const int LocalInput = 0;
    private const int Constant = 1;
    private const int LocalTableColumnStat = 3;
    private const int QuestionValue = 4;
    private const int QuestionTableColumnStat = 6;
    private const int FormulaResult = 8;

    public static async Task<Result<string>> ResolveAsync(
        ApplicationDbContext context,
        ResponseFormulaSnapshot snapshot,
        IFormulaCalculationClient calculationClient,
        CancellationToken cancellationToken,
        HashSet<Guid>? resolutionPath = null)
    {
        resolutionPath ??= [];
        if (!resolutionPath.Add(snapshot.Id))
            return FormulaResponseRuntimeErrors.BindingInvalid;
        try
        {
        var field = await context.FormFieldRevisions.AsNoTracking()
            .SingleOrDefaultAsync(item =>
                item.FormRevisionId == snapshot.Response.FormRevisionId &&
                item.PlacementKey == snapshot.PlacementKey, cancellationToken);
        if (field is null) return FormulaResponseRuntimeErrors.ConfigurationUnavailable;
        var stored = await context.FormResponses.AsNoTracking()
            .SingleOrDefaultAsync(item => item.ResponseId == snapshot.ResponseId &&
                item.FormFieldId == field.FormFieldId, cancellationToken);
        if (stored is null) return FormulaResponseRuntimeErrors.BindingInvalid;
            using var values = JsonDocument.Parse(stored.Value);
            using var bindings = JsonDocument.Parse(snapshot.BindingsJson);
            if (values.RootElement.ValueKind != JsonValueKind.Object ||
                bindings.RootElement.ValueKind != JsonValueKind.Array)
                return FormulaResponseRuntimeErrors.BindingInvalid;
            var calculatedCells = await FormulaWorksheetPreprocessor.ResolveAsync(
                values.RootElement, snapshot.ExecutableDefinitionJson,
                calculationClient, cancellationToken);
            if (calculatedCells.IsFailure)
                return Result.Failure<string>(calculatedCells.Errors);
            var resolved = new Dictionary<string, string?>();
            foreach (var binding in bindings.RootElement.EnumerateArray())
            {
                var variableKey = binding.GetProperty("variableKey").GetString();
                var source = binding.GetProperty("source");
                var sourceType = source.GetProperty("sourceType").GetInt32();
                var reference = source.GetProperty("reference").GetString();
                if (string.IsNullOrWhiteSpace(variableKey) || reference is null)
                    return FormulaResponseRuntimeErrors.BindingInvalid;
                var value = sourceType is QuestionValue or QuestionTableColumnStat
                    ? await FormulaCrossQuestionSourceResolver.ResolveAsync(
                        context, snapshot, source, cancellationToken)
                    : sourceType == FormulaResult
                        ? await FormulaResultSourceResolver.ResolveAsync(
                            context, snapshot, source, calculationClient,
                            cancellationToken, resolutionPath)
                    : ResolveValue(values.RootElement, source, variableKey,
                        calculatedCells.Value);
                if (value is null && sourceType is not LocalInput)
                    return FormulaResponseRuntimeErrors.BindingInvalid;
                resolved[variableKey] = value;
            }
            return FormulaCanonicalJson.Canonicalize(
                JsonSerializer.Serialize(resolved), 262_144);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or
                                      ArgumentException)
        {
            return FormulaResponseRuntimeErrors.BindingInvalid;
        }
        finally
        {
            resolutionPath.Remove(snapshot.Id);
        }
    }

    internal static string? ResolveValue(
        JsonElement values, JsonElement source, string variableKey,
        IReadOnlyDictionary<(int Row, int Column), string>? calculatedCells = null)
    {
        var sourceType = source.GetProperty("sourceType").GetInt32();
        var reference = source.GetProperty("reference").GetString();
        if (reference is null) return null;
        var value = sourceType switch
        {
            LocalInput => ReadLocal(values, variableKey, reference, calculatedCells),
            Constant => reference.StartsWith("constant:", StringComparison.Ordinal)
                ? reference[9..]
                : reference,
            LocalTableColumnStat => ReadTableStatistic(
                values, source, reference, calculatedCells),
            _ => null
        };
        if (value is null) return null;
        if (sourceType == LocalInput &&
            source.TryGetProperty("inputDecimalPlaces", out var inputPlaces) &&
            !FormulaDecimalStatistics.HasPermittedScale(
                value, inputPlaces.GetInt32())) return null;
        return value;
    }

    private static string? ReadLocal(
        JsonElement values,
        string variableKey,
        string reference,
        IReadOnlyDictionary<(int Row, int Column), string>? calculatedCells)
    {
        if (reference.StartsWith("local:manual:", StringComparison.Ordinal))
            return ReadManual(values, reference[13..]);
        if (reference.StartsWith("local:standardInput:", StringComparison.Ordinal))
            return ReadObjectScalar(values, "standardInputs", reference[20..]);
        if (reference.StartsWith("local:sampleInput:", StringComparison.Ordinal))
            return ReadSampleInput(values, reference[18..]);
        if (reference.StartsWith("local:cell:", StringComparison.Ordinal))
            return ReadCell(values, reference[11..], calculatedCells);
        var key = reference.StartsWith("local:", StringComparison.Ordinal)
            ? reference[6..]
            : variableKey;
        if (!values.TryGetProperty(key, out var value) || value.ValueKind == JsonValueKind.Null)
            return null;
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            _ => throw new InvalidOperationException("A scalar formula input is required.")
        };
    }

    private static string? ReadObjectScalar(JsonElement values, string property, string key)
    {
        if (!values.TryGetProperty(property, out var container) ||
            container.ValueKind != JsonValueKind.Object ||
            !container.TryGetProperty(key, out var value)) return null;
        return Scalar(value);
    }

    private static string? ReadManual(JsonElement values, string key)
    {
        if (!values.TryGetProperty("manualVars", out var manual) ||
            manual.ValueKind != JsonValueKind.Object ||
            !manual.TryGetProperty(key, out var value)) return null;
        if (value.ValueKind == JsonValueKind.Object)
        {
            if (value.TryGetProperty("evaluated", out var evaluated) &&
                evaluated.ValueKind != JsonValueKind.Null) return Scalar(evaluated);
            if (value.TryGetProperty("raw", out var raw)) return Scalar(raw);
        }
        return Scalar(value);
    }

    private static string? ReadSampleInput(JsonElement values, string path)
    {
        var parts = path.Split(':', 2);
        if (parts.Length != 2 || !int.TryParse(parts[0], out var index) || index < 0 ||
            !values.TryGetProperty("sampleInputs", out var samples) ||
            samples.ValueKind != JsonValueKind.Array || index >= samples.GetArrayLength())
            return null;
        var sample = samples[index];
        return sample.ValueKind == JsonValueKind.Object &&
               sample.TryGetProperty(parts[1], out var value) ? Scalar(value) : null;
    }

    private static string? ReadCell(
        JsonElement values, string path,
        IReadOnlyDictionary<(int Row, int Column), string>? calculatedCells)
    {
        var parts = path.Split(':', 2);
        if (parts.Length != 2 || !int.TryParse(parts[0], out var row) ||
            !int.TryParse(parts[1], out var column)) return null;
        return calculatedCells is not null && calculatedCells.TryGetValue((row, column), out var value)
            ? value
            : Cell(values, row, column);
    }

    private static string? ReadTableStatistic(
        JsonElement values, JsonElement source, string reference,
        IReadOnlyDictionary<(int Row, int Column), string>? calculatedCells)
    {
        const string prefix = "table:column:";
        if (!reference.StartsWith(prefix, StringComparison.Ordinal) ||
            !int.TryParse(reference[prefix.Length..], out var column) ||
            !FormulaTableColumnReader.TryRead(
                values, column, out var numbers, calculatedCells) ||
            !source.TryGetProperty("statistic", out var statistic))
            return null;
        return FormulaDecimalStatistics.Calculate(
            numbers, statistic.GetInt32(),
            OptionalInteger(source, "inputDecimalPlaces"),
            OptionalInteger(source, "tableCalculationDecimalPlaces"));
    }

    private static string? Cell(JsonElement values, int row, int column)
    {
        if (!values.TryGetProperty("tableData", out var table) ||
            table.ValueKind != JsonValueKind.Array || row < 0 || row >= table.GetArrayLength())
            return null;
        var cells = table[row];
        return cells.ValueKind == JsonValueKind.Array && column >= 0 &&
               column < cells.GetArrayLength() ? Scalar(cells[column]) : null;
    }

    private static string? Scalar(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => string.IsNullOrWhiteSpace(value.GetString()) ? null : value.GetString(),
        JsonValueKind.Number => value.GetRawText(),
        _ => null
    };

    private static int? OptionalInteger(JsonElement source, string property) =>
        source.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Number
            ? value.GetInt32()
            : null;
}
