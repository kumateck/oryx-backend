using System.Text.Json;
using SHARED;

#nullable enable

namespace APP.Services.Formulas;

internal static class FormulaWorksheetPreprocessor
{
    public static async Task<Result<IReadOnlyDictionary<(int Row, int Column), string>>> ResolveAsync(
        JsonElement storedValues,
        string executableDefinitionJson,
        IFormulaCalculationClient calculationClient,
        CancellationToken cancellationToken)
    {
        using var definition = JsonDocument.Parse(executableDefinitionJson);
        if (!definition.RootElement.TryGetProperty("worksheetPolicy", out var policy) ||
            !policy.TryGetProperty("preprocessor", out var packet))
            return Result.Success<IReadOnlyDictionary<(int, int), string>>(
                new Dictionary<(int, int), string>());

        var inputs = new Dictionary<string, string?>();
        foreach (var mapping in packet.GetProperty("inputs").EnumerateArray())
        {
            var key = mapping.GetProperty("variableKey").GetString();
            var row = mapping.GetProperty("row").GetInt32();
            var column = mapping.GetProperty("column").GetInt32();
            if (string.IsNullOrWhiteSpace(key) || !TryReadCell(storedValues, row, column, out var value))
                return FormulaResponseRuntimeErrors.BindingInvalid;
            inputs[key] = value;
        }

        var nested = packet.GetProperty("definition");
        var definitionHash = packet.GetProperty("definitionHash").GetString();
        var configurationHash = packet.GetProperty("configurationHash").GetString();
        var request = JsonSerializer.SerializeToElement(new
        {
            schemaVersion = "oryx-formula-service-evaluate-request-v1",
            requestId = Guid.NewGuid().ToString("N"),
            formulaLanguageVersion = nested.GetProperty("formulaLanguageVersion").GetString(),
            numericPolicyVersion = nested.GetProperty("numericPolicyVersion").GetString(),
            definitionHash,
            definition = nested,
            configurationHash,
            configuration = packet.GetProperty("configuration"),
            resolvedInputs = inputs
        });
        var evaluated = await calculationClient.EvaluateAsync(request, cancellationToken);
        if (evaluated.IsFailure) return Result.Failure<
            IReadOnlyDictionary<(int, int), string>>(evaluated.Errors);
        var response = evaluated.Value;
        if (response.Status != 5 || response.Evaluation is not { } evaluation ||
            response.ClaimedDefinitionHash != definitionHash ||
            response.ComputedDefinitionHash != definitionHash ||
            response.ClaimedConfigurationHash != configurationHash ||
            response.ComputedConfigurationHash != configurationHash ||
            response.PlacementKey != "worksheet-preprocessor" ||
            evaluation.GetProperty("status").GetInt32() != 0)
            return FormulaResponseRuntimeErrors.BindingInvalid;

        var results = evaluation.GetProperty("results").EnumerateArray().ToDictionary(
            item => item.GetProperty("key").GetString()!,
            item => item.GetProperty("roundedResult").GetString()!);
        var cells = new Dictionary<(int, int), string>();
        foreach (var output in packet.GetProperty("outputs").EnumerateArray())
        {
            var key = output.GetProperty("resultKey").GetString();
            var target = (output.GetProperty("row").GetInt32(),
                output.GetProperty("column").GetInt32());
            if (key is null || !results.TryGetValue(key, out var value) ||
                !cells.TryAdd(target, value))
                return FormulaResponseRuntimeErrors.BindingInvalid;
        }
        return Result.Success<IReadOnlyDictionary<(int, int), string>>(cells);
    }

    private static bool TryReadCell(
        JsonElement values, int row, int column, out string? value)
    {
        value = null;
        if (!values.TryGetProperty("tableData", out var table) ||
            table.ValueKind != JsonValueKind.Array || row < 0 || row >= table.GetArrayLength())
            return false;
        var cells = table[row];
        if (cells.ValueKind != JsonValueKind.Array || column < 0 ||
            column >= cells.GetArrayLength()) return false;
        var cell = cells[column];
        value = cell.ValueKind switch
        {
            JsonValueKind.String when !string.IsNullOrWhiteSpace(cell.GetString()) => cell.GetString(),
            JsonValueKind.Number => cell.GetRawText(),
            _ => null
        };
        return value is not null;
    }
}
