using System.Globalization;
using System.Text.Json;
using DOMAIN.Entities.Forms;
using SHARED;

#nullable enable
namespace APP.Services.Forms.Microbial;

/// <summary>Reads, checks and calculates the answers of the microbial question types. Pure: no database access.</summary>
public static class MicrobialValues
{
    public static bool IsMicrobial(QuestionType type) => type is QuestionType.Organism or QuestionType.Dilution or QuestionType.Incubation
        or QuestionType.ColonyCount or QuestionType.CfuCalculation or QuestionType.GrowthObservation;

    public sealed record DilutionValue(decimal? Sample, string? SampleUnit, decimal? Diluent, decimal Factor);
    public sealed record IncubationValue(decimal Temperature, decimal Hours, DateTime StartedAt, DateTime? EndedAt);
    public sealed record ColonyCountValue(IReadOnlyList<int> Plates, bool TooNumerousToCount)
    {
        public decimal? Average => TooNumerousToCount || Plates.Count == 0 ? null : (decimal)Plates.Sum() / Plates.Count;
    }
    public sealed record CfuValue(decimal? Average, decimal Factor, decimal? Result, bool TooNumerousToCount, string Unit);

    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    /// <summary>Checks an answer an analyst saves. Null value means "cleared" and is allowed; CfuCalculation answers are written by the server.</summary>
    public static Result Validate(QuestionType type, string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || !IsMicrobial(type) || type == QuestionType.CfuCalculation) return Result.Success();
        return type switch
        {
            QuestionType.Organism => MicrobialOrganisms.IsKnown(value.Trim()) ? Result.Success() : Invalid("Choose an organism from the list."),
            QuestionType.GrowthObservation => value.Trim() is "Absent" or "Present" ? Result.Success() : Invalid("Record growth as Absent or Present."),
            QuestionType.Dilution => ReadDilution(value) is not null ? Result.Success() : Invalid("Enter a dilution factor greater than zero."),
            QuestionType.Incubation => ReadIncubation(value) is { } incubation && (incubation.EndedAt is null || incubation.EndedAt >= incubation.StartedAt)
                ? Result.Success() : Invalid("Enter the incubation temperature, period and start time; the end cannot be before the start."),
            QuestionType.ColonyCount => ReadColonyCount(value) is not null ? Result.Success() : Invalid("Enter each plate count as a whole number of zero or more, or mark the plates too numerous to count."),
            _ => Result.Success(),
        };
    }

    private static Result Invalid(string message) => Error.Validation("Response.MicrobialAnswer", message);

    /// <summary>A CFU calculation names its unit as its one option; the other microbial types have no options.</summary>
    public static Result ValidateQuestion(QuestionType type, IReadOnlyCollection<string?> options)
    {
        if (!IsMicrobial(type)) return Result.Success();
        if (type == QuestionType.CfuCalculation)
            return options.Count == 1 && MicrobialUnits.Cfu.Contains(options.First()?.Trim() ?? "") ? Result.Success()
                : Error.Validation("Question.MicrobialUnit", $"A CFU calculation needs exactly one unit: {string.Join(", ", MicrobialUnits.Cfu)}.");
        return options.Count == 0 ? Result.Success() : Error.Validation("Question.MicrobialOptions", "This microbial question type has no options.");
    }

    /// <summary>Whether an answer is complete enough to submit.</summary>
    public static bool IsComplete(QuestionType type, string? value) => !string.IsNullOrWhiteSpace(value) && type switch
    {
        QuestionType.Incubation => ReadIncubation(value) is { EndedAt: not null } incubation && incubation.EndedAt >= incubation.StartedAt,
        QuestionType.CfuCalculation => ReadCfu(value) is not null,
        _ => Validate(type, value).IsSuccess,
    };

    public static DilutionValue? ReadDilution(string? value) => Read(value, root =>
    {
        var factor = Number(root, "factor");
        if (factor is not > 0) return null;
        var sample = Number(root, "sample");
        var diluent = Number(root, "diluent");
        if (sample is < 0 || diluent is < 0) return null;
        return new DilutionValue(sample, Text(root, "sampleUnit"), diluent, factor.Value);
    });

    public static IncubationValue? ReadIncubation(string? value) => Read(value, root =>
    {
        var temperature = Number(root, "temperature");
        var hours = Number(root, "hours");
        var started = Date(root, "startedAt");
        if (temperature is null || hours is not > 0 || started is null) return null;
        return new IncubationValue(temperature.Value, hours.Value, started.Value, Date(root, "endedAt"));
    });

    public static ColonyCountValue? ReadColonyCount(string? value) => Read(value, root =>
    {
        var tntc = root.TryGetProperty("tooNumerousToCount", out var flag) && flag.ValueKind == JsonValueKind.True;
        var plates = new List<int>();
        if (root.TryGetProperty("plates", out var array) && array.ValueKind == JsonValueKind.Array)
            foreach (var item in array.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Number || !item.TryGetInt32(out var count) || count < 0) return null;
                plates.Add(count);
            }
        return tntc || plates.Count > 0 ? new ColonyCountValue(plates, tntc) : null;
    });

    public static CfuValue? ReadCfu(string? value) => Read(value, root =>
    {
        var factor = Number(root, "factor");
        var unit = Text(root, "unit");
        if (factor is not > 0 || string.IsNullOrWhiteSpace(unit)) return null;
        var tntc = root.TryGetProperty("tooNumerousToCount", out var flag) && flag.ValueKind == JsonValueKind.True;
        var result = Number(root, "result");
        if (!tntc && result is null) return null;
        return new CfuValue(Number(root, "average"), factor.Value, result, tntc, unit);
    });

    /// <summary>Average count × dilution factor. Too-numerous plates give no number and never comply with a count limit.</summary>
    public static CfuValue Calculate(ColonyCountValue count, DilutionValue dilution, string unit)
    {
        var average = count.Average;
        return new CfuValue(average, dilution.Factor, average is null ? null : average.Value * dilution.Factor, count.TooNumerousToCount, unit);
    }

    public static string Serialize(CfuValue value) => JsonSerializer.Serialize(new
    {
        average = value.Average, factor = value.Factor, result = value.Result, tooNumerousToCount = value.TooNumerousToCount, unit = value.Unit,
    }, Web);

    private static T? Read<T>(string? value, Func<JsonElement, T?> read) where T : class
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        try
        {
            using var document = JsonDocument.Parse(value);
            return document.RootElement.ValueKind == JsonValueKind.Object ? read(document.RootElement) : null;
        }
        catch (JsonException) { return null; }
    }

    private static decimal? Number(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var item)) return null;
        if (item.ValueKind == JsonValueKind.Number && item.TryGetDecimal(out var number)) return number;
        return item.ValueKind == JsonValueKind.String && decimal.TryParse(item.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;
    }

    private static string? Text(JsonElement root, string name) =>
        root.TryGetProperty(name, out var item) && item.ValueKind == JsonValueKind.String ? item.GetString()?.Trim() : null;

    private static DateTime? Date(JsonElement root, string name) =>
        root.TryGetProperty(name, out var item) && item.ValueKind == JsonValueKind.String
        && DateTime.TryParse(item.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var date) ? date : null;
}
