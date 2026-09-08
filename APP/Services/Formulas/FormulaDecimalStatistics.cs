using System.Globalization;

#nullable enable

namespace APP.Services.Formulas;

internal static class FormulaDecimalStatistics
{
    private const int MaximumDecimalPlaces = 12;

    public static string? Calculate(
        IEnumerable<string> rawValues,
        int statistic,
        int? inputDecimalPlaces,
        int? calculationDecimalPlaces)
    {
        if (!ValidPlaces(inputDecimalPlaces) || !ValidPlaces(calculationDecimalPlaces))
            return null;
        var values = new List<decimal>();
        foreach (var raw in rawValues)
        {
            if (string.IsNullOrWhiteSpace(raw)) continue;
            if (inputDecimalPlaces.HasValue &&
                !HasPermittedScale(raw, inputDecimalPlaces.Value)) return null;
            if (!decimal.TryParse(raw, NumberStyles.Float,
                    CultureInfo.InvariantCulture, out var value)) return null;
            values.Add(value);
        }
        if (values.Count == 0) return null;
        try
        {
            var mean = values.Sum() / values.Count;
            var result = statistic switch
            {
                0 => mean,
                1 => StandardDeviation(values, mean, sample: true),
                2 => StandardDeviation(values, mean, sample: false),
                3 => mean == 0 ? null :
                    StandardDeviation(values, mean, sample: true) /
                    Math.Abs(mean) * 100m,
                4 => values.Min(),
                5 => values.Max(),
                6 => values.Count,
                _ => null
            };
            if (!result.HasValue) return null;
            var rounded = calculationDecimalPlaces.HasValue
                ? Math.Round(result.Value, calculationDecimalPlaces.Value,
                    MidpointRounding.AwayFromZero)
                : result.Value;
            return rounded.ToString("0.############################",
                CultureInfo.InvariantCulture);
        }
        catch (OverflowException)
        {
            return null;
        }
    }

    public static bool HasPermittedScale(string value, int maximum)
    {
        if (!ValidPlaces(maximum)) return false;
        var exponent = value.IndexOfAny(['e', 'E']);
        var mantissa = exponent < 0 ? value : value[..exponent];
        var point = mantissa.IndexOf('.');
        var scale = point < 0 ? 0 : mantissa.Length - point - 1;
        if (exponent >= 0)
        {
            if (!int.TryParse(value[(exponent + 1)..], NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out var power)) return false;
            scale = Math.Max(0, scale - power);
        }
        return scale <= maximum;
    }

    private static decimal? StandardDeviation(
        IReadOnlyCollection<decimal> values, decimal mean, bool sample)
    {
        if (values.Count == 1) return 0m;
        var squares = values.Sum(value => (value - mean) * (value - mean));
        var denominator = sample ? values.Count - 1 : values.Count;
        return SquareRoot(squares / denominator);
    }

    private static decimal SquareRoot(decimal value)
    {
        if (value <= 0) return 0;
        var estimate = (decimal)Math.Sqrt((double)value);
        if (estimate == 0) estimate = 1;
        for (var iteration = 0; iteration < 32; iteration++)
        {
            var next = (estimate + value / estimate) / 2m;
            if (next == estimate) break;
            estimate = next;
        }
        return estimate;
    }

    private static bool ValidPlaces(int? places) =>
        !places.HasValue || ValidPlaces(places.Value);

    private static bool ValidPlaces(int places) =>
        places is >= 0 and <= MaximumDecimalPlaces;
}
